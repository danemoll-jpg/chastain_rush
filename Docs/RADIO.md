# Chastain Rush radio

Woodstock Rush's local radio, ported in Round 27. It plays songs from folders on this computer. Nothing is downloaded or uploaded.

Fresh settings use **Music beside ChastainRush.exe** (Bundled). The build ships the songs staged in the repo's `BundleMusic/`
folder there. Settings > Audio > Music > Music Library switches between Bundled and a custom folder.

Each folder directly inside the music folder is one station, named after that folder. Songs in nested artist or album folders stay in
that station. Songs directly in the music folder form a station called General. Empty folders are left out, unreadable songs are
skipped, and a station with no playable songs is skipped. Stations are sorted by folder name; General comes first when present.

```
Music/Rock/Artist/Album/song.mp3   -> Rock
Music/Country/song.mp3             -> Country
Music/song.wav                     -> General
```

## Buttons while driving (free roam or a race)

| | Controller | Keyboard |
|---|---|---|
| Next station, then Off | D-pad down | N |
| Next song (shuffled) | D-pad right | . (period) |
| Previous song | D-pad left | , (comma) |
| Show the station and song | D-pad up | I |

Woodstock Rush uses `[` and `]` for previous / next song. In Chastain Rush those keys are the debug panel and bug capture, so the radio
uses `,` and `.` instead. The controller buttons are the same as Woodstock Rush. The radio does not react while a menu, the map or the
debug menu is open.

Settings > Audio also has the station, previous / next and the music volume. Off stops only the radio. Master volume affects all sound.

## Formats and limits

DRM-free MP3, PCM WAV and Ogg Vorbis. Limits per scan: 100,000 songs, 250,000 folder entries, 20,000 folders, 30 seconds.
Limits per file: WAV 64 MiB, MP3 and Ogg 256 MiB. Folder links are skipped.

## Adding songs to the build

1. Copy the songs into the repo's `BundleMusic/` folder, one folder per station (for example `BundleMusic/Rock/song.mp3`).
2. Build the exe (Chastain Rush > Build Executable). The build copies everything in `BundleMusic/` into `Builds/ChastainRush/Music/`,
   writes `Music/README.txt`, and puts this file beside the exe. Songs in `Music/` that are not in `BundleMusic/` are removed, so the
   build ships exactly what is staged.

A custom music folder (Settings > Audio > Music > Music Library) is never copied anywhere. Its default is the `Music` folder beside the
records (`%USERPROFILE%\AppData\LocalLow\Chastain Rush\Chastain Rush\Music`).
