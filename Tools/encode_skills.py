"""Encode six actual Unity ultimate effects with their generated game audio cues; not a frame-rate benchmark."""
import array
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import wave

import imageio_ffmpeg

ROOT = Path(__file__).resolve().parent.parent


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def current_report():
    build = read(ROOT / "BuildLogs/windows-build.json")
    for path in sorted((ROOT / "BuildLogs").glob("PlayerChecks-*/report.json"), key=lambda p: p.stat().st_mtime, reverse=True):
        report = read(path)
        if report["passed"] and report.get("guid") == build["guid"] and (path.parent / "skill-effects-movie.json").exists():
            return path, build
    raise RuntimeError("No passing replay from the current build")


def main():
    report_path, build = current_report()
    report = read(report_path)
    folder = report_path.parent
    review = read(folder / "skill-effects-movie.json")
    frames = folder / "SkillFrames"
    assert review["fps"] == 30 and review["frames"] == 432 and review["sound_cues"] > 5
    assert len(list(frames.glob("*.jpg"))) == review["frames"]
    wav = folder / "skill-effects-review.wav"
    with wave.open(str(wav), "rb") as audio:
        assert audio.getnchannels() == 1 and audio.getsampwidth() == 2
        duration = audio.getnframes() / audio.getframerate()
        pcm = array.array("h", audio.readframes(audio.getnframes()))
    rms = (sum(value * value for value in pcm) / len(pcm)) ** .5 / 32768
    assert duration >= 14.4 and rms > .001, "Silent or truncated review audio"
    movie = folder / "skill-effects-review.mp4"
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    result = subprocess.run([ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-framerate", "30", "-i", str(frames / "%04d.jpg"), "-i", str(wav), "-vf", "scale=1280:-2", "-c:v", "libx264", "-preset", "fast", "-crf", "23", "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "96k", "-shortest", "-movflags", "+faststart", str(movie)], capture_output=True, text=True)
    if result.returncode:
        raise RuntimeError(result.stderr[-3000:])
    subprocess.run([ffmpeg, "-hide_banner", "-loglevel", "error", "-i", str(movie), "-f", "null", "-"], check=True)
    metadata = {
        **review, "version": build["version"], "guid": build["guid"],
        "device": report["device"], "report": report_path.relative_to(ROOT).as_posix(),
        "movie_sha256": digest(movie), "wav_sha256": digest(wav),
        "audio_seconds": duration, "audio_rms": round(rms, 6),
        "audio_scope": "The same in-game effect clips mixed at their replay cue times; not hardware audio capture.",
        "decode_passed": True,
    }
    (folder / "skill-effects-review-encoded.json").write_text(json.dumps(metadata, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"movie": str(movie), "bytes": movie.stat().st_size, "frames": review["frames"], "sound_cues": review["sound_cues"], "audio_rms": rms}))


if __name__ == "__main__":
    main()
