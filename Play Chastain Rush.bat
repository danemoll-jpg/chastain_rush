@echo off
rem Starts the built game. Build it first: in Unity, Chastain Rush > Build Executable (or see CLAUDE.md for the batch command).
rem Ported from Racer's Play-Racer.cmd (which starts its launcher); Chastain Rush has no launcher, so this starts the exe.
setlocal
set "GAME=%~dp0Builds\ChastainRush"
if not exist "%GAME%\ChastainRush.exe" (
  echo Chastain Rush has not been built yet. In Unity: Chastain Rush ^> Build Executable.
  pause
  exit /b 1
)
start "" /D "%GAME%" "%GAME%\ChastainRush.exe"
