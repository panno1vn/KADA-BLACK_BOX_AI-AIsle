# Emotion Recognition (demo — internal testing only)

Dịch vụ Python nhận diện cảm xúc gương mặt từ camera, mô phỏng ý tưởng "camera đặt ở quầy thu ngân". Dùng [DeepFace](https://github.com/serengil/deepface) (RetinaFace để tìm mặt + model cảm xúc pretrained, phân loại 7 loại: angry, disgust, fear, happy, sad, surprise, neutral) — không train model nào từ đầu, chỉ tải model có sẵn.

> **Phạm vi hiện tại: demo/thử nghiệm nội bộ, không phải tính năng sản phẩm thật.** `docs/Result_Plan.md` (mục Data Privacy) đang ghi rõ *"Mặc định không triển khai: face recognition, customer identity recognition"*. Nhận diện cảm xúc không cần biết danh tính khách, nhưng vẫn xử lý ảnh khuôn mặt thật — nếu sau này muốn đưa vào sản phẩm thật (camera theo dõi khách hàng thật liên tục), cần quay lại cập nhật chính sách privacy trước, không âm thầm mở rộng phạm vi.

## Không lưu ảnh

Mỗi frame gửi lên được giải mã trong bộ nhớ, phân tích, rồi bỏ — service này **không ghi ảnh nào xuống đĩa**, chỉ trả về nhãn cảm xúc.

## Cài đặt

```powershell
cd e:\AIsle
venv\Scripts\python.exe -m pip install -r services\VideoAnalytics\EmotionRecognition\requirements.txt
```

Lần chạy đầu tiên, DeepFace tự tải 2 file model (RetinaFace ~119MB, model cảm xúc ~6MB) từ GitHub releases về `~/.deepface/weights/` — cần mạng, chỉ tải 1 lần.

## Chạy service

```powershell
venv\Scripts\python.exe -m uvicorn services.VideoAnalytics.EmotionRecognition.emotion_service:app --host 127.0.0.1 --port 8801
```

## Thử bằng webcam

Mở trực tiếp `services/VideoAnalytics/EmotionRecognition/demo.html` bằng trình duyệt (double-click, không cần server riêng cho file này) — cho phép quyền camera, sẽ tự chụp và gửi phân tích mỗi ~2.2 giây.

## API

`POST /analyze` — multipart form field `file` là 1 ảnh (jpg/png). Trả về:

```json
{
  "faceCount": 1,
  "faces": [{
    "dominantEmotion": "happy",
    "scores": {"happy": 90.4, "neutral": 9.44, "angry": 0.07, "sad": 0.04, "surprise": 0.04, "fear": 0.0, "disgust": 0.0},
    "region": {"x": 213, "y": 189, "w": 138, "h": 200, "left_eye": [332, 271], "right_eye": [271, 270], ...}
  }]
}
```

`faceCount: 0` khi không thấy khuôn mặt nào trong khung hình (không báo lỗi).

## Hiệu năng

Chạy CPU (chưa dùng GPU): ~1.8 giây/lần phân tích trên máy phát triển — đủ cho demo, không phải video real-time mượt. Muốn nhanh hơn cần cài TensorFlow bản GPU hoặc đổi backend detector nhẹ hơn (`opencv`, `ssd` — nhưng bản OpenCV 5.x hiện thiếu file haarcascade nên tạm không dùng được backend `opencv`).

## Lỗi thường gặp

- **"An exception occurred while downloading facial_expression_model_weights.h5..."** dù mạng bình thường: đây thực chất là lỗi encode emoji trên console Windows (`cp1252` không encode được emoji trong log của DeepFace), bị nuốt và báo nhầm thành lỗi tải file. Đã vá sẵn trong `emotion_service.py` (ép stdout/stderr sang UTF-8) — nếu vẫn gặp khi chạy script Python khác ngoài service này, thêm `PYTHONIOENCODING=utf-8` vào biến môi trường trước khi chạy.
- **"Confirm that opencv is installed... haarcascade_frontalface_default.xml"**: `opencv-python` 5.x không kèm sẵn file haarcascade. Service này đã cấu hình dùng `detector_backend="retinaface"` để tránh phụ thuộc file đó — không cần sửa gì thêm.
