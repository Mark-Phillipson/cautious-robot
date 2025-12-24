# Picture Game Static Assets

This folder contains curated images for the Picture Guess game.

Guidelines:
- Filenames should be lowercase and indicate the word, e.g. `dog.svg`.
- Recommended size: 512x512 (PNG/WebP/SVG). Optimize images for web and compress.
- Include metadata in `manifest.json` to map words to filenames and record license/source.
- Prefer permissive licenses (CC0, CC-BY) or your own created assets.

Manifest format (`manifest.json`):
```
{
  "entries": [
    { "word": "Dog", "fileName": "dog.svg", "license": "CC0", "source": "placeholder" }
  ]
}
```

Steps to add images:
1. Add the image file to this folder (prefer WebP or optimized PNG/SVG).
2. Add an entry to `manifest.json` with `word` and `fileName`.
3. Commit and push. The app will load the manifest at startup.

Tips:
- Use lowercased single words only; avoid spaces/special characters.
- If you need a bulk downloader, use a script to download and name files consistently and then generate the manifest file.
- Consider image deduplication and size limits to control repo size.
