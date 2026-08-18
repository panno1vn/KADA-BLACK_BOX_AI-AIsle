"""
Facial emotion recognition demo service (internal testing only — see README.md in this folder).

Not part of the store-wide anonymous person-tracking pipeline planned in docs/Result_Plan.md.
No frame is ever written to disk: each uploaded image is decoded in memory, analyzed, and
discarded — only the derived emotion label/scores leave this process.
"""
from __future__ import annotations

import logging
import sys

# DeepFace's logger prints emoji (e.g. the download-progress message). Windows' console
# defaults to the cp1252 codepage, which can't encode them — that crash gets swallowed by
# DeepFace's own except-and-rewrap logic and surfaces as a misleading "download failed" error.
if sys.platform == "win32":
    sys.stdout.reconfigure(encoding="utf-8")
    sys.stderr.reconfigure(encoding="utf-8")

import cv2
import numpy as np
from deepface import DeepFace
from fastapi import FastAPI, File, HTTPException, UploadFile
from fastapi.middleware.cors import CORSMiddleware

logging.getLogger("tensorflow").setLevel(logging.ERROR)
logger = logging.getLogger("emotion_service")

MAX_UPLOAD_BYTES = 8_000_000  # a 480x360 webcam JPEG is a few hundred KB; this is a generous ceiling

# DeepFace classifies 7 raw expressions; the web tab only wants 3 buckets by valence.
# Surprise is valence-ambiguous (a good deal vs. a bad price both trigger it) so it's
# grouped as neutral rather than guessed as positive or negative.
EMOTION_GROUP = {
    "happy": "vui",
    "sad": "buon", "angry": "buon", "disgust": "buon", "fear": "buon",
    "neutral": "trung_tinh", "surprise": "trung_tinh",
}

app = FastAPI(title="AIsle Emotion Recognition (demo)")
# Only the app's own web origin and the local demo.html (browsers send Origin: null for
# file:// pages) may call this — not a public API, no reason to accept requests from
# any website a visitor happens to have open.
app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://127.0.0.1:8765", "http://localhost:8765", "null"],
    allow_methods=["GET", "POST"],
    allow_headers=["*"],
)


@app.get("/health")
def health():
    return {"ok": True, "service": "emotion-recognition-demo"}


@app.post("/analyze")
async def analyze(file: UploadFile = File(...)):
    raw = await file.read(MAX_UPLOAD_BYTES + 1)
    if len(raw) > MAX_UPLOAD_BYTES:
        raise HTTPException(status_code=413, detail="Image too large")
    frame = cv2.imdecode(np.frombuffer(raw, dtype=np.uint8), cv2.IMREAD_COLOR)
    if frame is None:
        raise HTTPException(status_code=400, detail="Could not decode image")

    try:
        # opencv-python 5.x ships without its bundled Haar cascade data files, so the default
        # 'opencv' detector backend fails outright — retinaface doesn't depend on that data.
        results = DeepFace.analyze(frame, actions=["emotion"], detector_backend="retinaface", enforce_detection=False)
    except Exception:
        # Log the real cause server-side only — a raw exception message (which has included
        # local filesystem paths in practice, e.g. the model weight cache dir) shouldn't go
        # to every caller.
        logger.exception("DeepFace.analyze failed")
        raise HTTPException(status_code=500, detail="Analysis failed") from None

    faces = results if isinstance(results, list) else [results]
    # DeepFace still returns a low-confidence guess with enforce_detection=False even when no
    # real face was found; filter those out rather than reporting a fake emotion for an empty frame.
    faces = [f for f in faces if f.get("face_confidence", 0) > 0]

    return {
        "faceCount": len(faces),
        "faces": [simplify(f) for f in faces],
    }


def simplify(face: dict) -> dict:
    grouped = {"vui": 0.0, "buon": 0.0, "trung_tinh": 0.0}
    for raw_emotion, value in face["emotion"].items():
        grouped[EMOTION_GROUP[raw_emotion]] += float(value)  # numpy.float32 isn't JSON-serializable
    scores = {k: round(v, 2) for k, v in grouped.items()}
    return {
        "dominantEmotion": max(scores, key=scores.get),
        "scores": scores,
        "region": {k: (int(v) if isinstance(v, (int, float)) else v) for k, v in face["region"].items()},
    }
