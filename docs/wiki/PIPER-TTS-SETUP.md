# Piper TTS Setup

MOBAflow supports local text-to-speech announcements with Piper TTS. Piper runs
offline, does not require an API key, and is suitable for open-source usage.

## Install Piper

The recommended open-source distribution is
[`OHF-Voice/piper1-gpl`](https://github.com/OHF-Voice/piper1-gpl):

```powershell
py -m pip install piper-tts
```

After installation, use the generated `piper.exe` from your Python environment
or virtual environment, for example `.venv\Scripts\piper.exe`.

1. Install Piper with `py -m pip install piper-tts`.
2. Download a German voice model from <https://huggingface.co/rhasspy/piper-voices>.
3. Keep the `.onnx` model and optional `.onnx.json` config file in a folder you can reference from MOBAflow.
4. Advanced users may also use a standalone Piper binary if it supports the same CLI options.

## Configure MOBAflow

1. Open **Settings → Speech Synthesis**.
2. Select **Piper TTS**.
3. Set **Piper Executable** to the local `piper.exe`.
4. Set **Piper Model** to the downloaded `.onnx` voice model.
5. Optionally set **Piper Config** to the matching `.json` config file.
6. Click **Test Speech**.

## Example Configuration

```json
{
  "Speech": {
    "PiperExecutablePath": "C:\\Tools\\piper\\piper.exe",
    "PiperModelPath": "C:\\Tools\\piper\\voices\\de_DE-thorsten-medium.onnx",
    "PiperConfigPath": "",
    "Rate": -1,
    "Volume": 90,
    "SpeakerEngineName": "Piper TTS",
    "VoiceName": "",
    "TestMessage": "Dies ist ein Test der Sprachsynthese. Nächster Halt: Hauptbahnhof."
  }
}
```

## Fallback

If Piper is selected but the executable or model path is missing, MOBAflow falls
back to **System Speech (Windows SAPI)** and logs a warning. You can also select
Windows SAPI directly in **Settings → Speech Synthesis**.

## Troubleshooting

- If the status says **Not Configured**, check the `piper.exe` and model paths.
- If startup fails, run `piper.exe --help` in a terminal to verify the executable.
- If no audio is heard, use **Test Speech** with Windows SAPI to verify the local audio device.
