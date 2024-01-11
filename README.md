# newsVideoGenerator

# WIP 👷‍

### A simple tool generating short form videos based of news articles

## Warning!

This project is still being developed so for example subtitles can be out of sync

## Requirements

- openAI API key
- ffmpeg.exe
- background video (preferably in vertical format)

## Using

```
.\newsVideoGenerator.exe <path to the config file>
```

no argument required if config file is named config.json and is in root directory

## Example config.json

```

{
  "articles": [
    "<url 1>",
    "<url 2>"
  ],
  "videoDirectory": "<directory to the background video>",
  "gptModel": "Gpt_3_5_Turbo",
  "ttsModel": "Tts_1_hd",
  "ttsVoice": "Echo",
  "ttsSpeed": 1.2,
  "gptPrompt": "Write a short summary of this articles that will be presented as a short form (under a minute) video on tiktok or youtube shorts. The video must be under a minute, about 140 words. Write only the script, NO hashtags, NO greatings, NO `???`, NO `#' etc. Remember to DO NOT ADD # hashtags at the end. End with a sentence that can lead to the starting sentence. Something like `And thats why'",
  "ffmpegDirectory": "<directory to ffmpeg.exe>",
  "openaiAPI": "<your openAI API key>"
}

```

## .env

if you for some reson don't want to place your API key in the config file, you can use .env

```
OPENAI_API=<your openAI API key>
```
