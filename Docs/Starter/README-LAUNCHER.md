# Chastain Rush — complete download

This ZIP holds the launcher, the game, the soundtrack, the cover art, and the icon and poster. The launcher keeps the game up to date: each time you open it, it checks for a newer version and installs it before the game starts, so you only install once.

## Windows

1. Extract the ZIP anywhere you can write to (for example `Documents\Games`). You get a `ChastainRush` folder.
2. Run `ChastainRush\ChastainRushLauncher.exe`.
3. Leave it alone: it checks for updates, downloads and installs a newer version if there is one (the window shows the progress), then starts the game. Without an internet connection, or if the check fails, the installed game starts.

B / Esc skips the update and plays the installed game at once. Any other button or key opens the launcher's menu instead: **Check for updates**, **Update game and play**, **Update soundtrack**, **Restore previous version**. D-pad / arrows choose, A / Enter confirms, B / Esc cancels.

## Steam Deck

1. Switch to **Desktop Mode**.
2. Download the ZIP and extract it anywhere in your home folder (for example `/home/deck/Games`), or on the SD card. You get a `ChastainRush` folder.
3. In Steam: **Games → Add a Non-Steam Game to My Library → Browse**, pick `ChastainRush/ChastainRushLauncher.exe`, then **Add Selected Programs**.
4. Right-click the new entry → **Properties → Compatibility** → tick **Force the use of a specific Steam Play compatibility tool** → **Proton Experimental**.
5. Optional: in the shortcut's **Properties**, click the icon square and pick `ChastainRush/ChastainRush-Icon.png`. For the library artwork, right-click the game in the Library (Desktop Mode) → **Manage → Set custom artwork** and pick `ChastainRush/ChastainRush-Poster.png`. Both are in the top folder, beside the launcher.
6. Back in Gaming Mode, play it from your library. The launcher updates the game by itself, then starts it. The game opens full screen; on the Deck its **Steam Deck** setting (Settings → Display) is on by itself (a shorter view, less traffic and fewer people, for the frame rate).

Keep using this same Steam shortcut from now on. Your saves and settings live in that shortcut's Proton prefix; a new shortcut starts with a new, empty prefix.

## Your files

- `shared/PersonalMusic`: put your own songs here (subfolders become radio stations). Updates never touch it.
- `versions`: the installed game, plus the previous one so you can go back. Saves are not stored here, and an update or a restore never removes them. A backup copy of the saves is made before each update, in `save-backups`.
- `recovery.log`: what the launcher did, if something goes wrong.

Close the game before you install an update.

The map is © OpenStreetMap contributors.
