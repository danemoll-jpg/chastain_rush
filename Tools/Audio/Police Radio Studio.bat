@echo off
cd /d "%~dp0"
where py >nul 2>nul && (start "" pyw -3 police_radio_studio.py) || (start "" pythonw police_radio_studio.py)
