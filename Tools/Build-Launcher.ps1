# Round 81 (J): builds the launcher (Launcher/, Woodstock Rush's, renamed for Chastain Rush) with Visual Studio's C++ compiler.
# Builds/Launcher/ChastainRushLauncher.exe, or with -CoreOnly the console LauncherChecks.exe (verify / status).
# The Windows SDK headers and libraries are read from Woodstock Rush's Builds/LauncherSDK (read only; -Sdk <folder> for another copy).
param([switch]$CoreOnly, [string]$Sdk = 'C:\Users\danmo\Racer\Builds\LauncherSDK')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$out=Join-Path $root 'Builds/Launcher'
New-Item -ItemType Directory -Force $out, (Join-Path $root 'Temp') | Out-Null
$vc='C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars64.bat'
$script=Join-Path $root 'Temp/build-launcher.cmd'
$entry=if($CoreOnly){'core_cli.cpp'}else{'launcher.cpp'}
$subsystem=if($CoreOnly){'CONSOLE'}else{'WINDOWS'}
$exe=if($CoreOnly){'LauncherChecks.exe'}else{'ChastainRushLauncher.exe'}
$rc=Join-Path $Sdk 'microsoft.windows.sdk.cpp/c/bin/10.0.28000.0/x64/rc.exe'
$rcStep=if($CoreOnly){''}else{"`"$rc`" /nologo /fo `"$out\launcher.res`" `"$root\Launcher\launcher.rc`"`r`nif errorlevel 1 exit /b %errorlevel%"}
$res=if($CoreOnly){''}else{"`"$out\launcher.res`""}
@"
@echo off
call "$vc" >nul
if errorlevel 1 exit /b %errorlevel%
set "INCLUDE=$Sdk\microsoft.windows.sdk.cpp\c\Include\10.0.28000.0\ucrt;$Sdk\microsoft.windows.sdk.cpp\c\Include\10.0.28000.0\shared;$Sdk\microsoft.windows.sdk.cpp\c\Include\10.0.28000.0\um;%INCLUDE%"
set "LIB=$Sdk\microsoft.windows.sdk.cpp.x64\c\um\x64;$Sdk\microsoft.windows.sdk.cpp.x64\c\ucrt\x64;%LIB%"
cl /nologo /TC /O2 /MT /c /Fo:"$out\miniz.obj" "$root\Launcher\vendor\miniz.c"
if errorlevel 1 exit /b %errorlevel%
$rcStep
cl /nologo /utf-8 /std:c++17 /EHsc /O2 /MT /W4 /Fe:"$out\$exe" /Fo:"$out\\" "$root\Launcher\$entry" "$root\Launcher\core.cpp" "$root\Launcher\store.cpp" "$out\miniz.obj" $res /link /SUBSYSTEM:$subsystem
"@ | Set-Content -LiteralPath $script
$ErrorActionPreference='Continue'; & cmd.exe /d /c $script 2>&1 | Out-Host; $code=$LASTEXITCODE; $ErrorActionPreference='Stop'   # (vcvars writes a harmless vswhere line to stderr)
if($code -ne 0){throw "Launcher compilation failed: $code"}
Get-FileHash -LiteralPath "$out/$exe"
