export function createSimulationMusic({src = 'assets/audio/music.mp3', audioFactory = value => new Audio(value), warn = message => console.warn(message)} = {}) {
  const audio = audioFactory(src);
  let warned = false;
  audio.loop = true;
  audio.volume = 1.0;
  const warnOnce = error => {
    if (warned) return;
    warned = true;
    warn(`Không thể phát nhạc mô phỏng: ${error?.message || error || 'unknown error'}`);
  };
  audio.addEventListener?.('error', warnOnce);
  return {
    audio,
    enter() {
      try {
        const result = audio.play();
        result?.catch?.(warnOnce);
      } catch (error) { warnOnce(error); }
    },
    leave() { audio.pause(); },
  };
}
