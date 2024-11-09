@echo off
setlocal enabledelayedexpansion

SET installerDir="%~dp0"

REM make sure we're not running within a zip folder
REM The error level is usually 0 (install dir contains temp path), 1 (it doesn't), or 9009 (findstr doesn't exist due to a Windows issue).
REM If the command doesn't exist, just skip this check.
echo %installerDir% | findstr /C:"%TEMP%" 1>nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo Oops! It looks like you're running the installer from inside a zip file. Make sure you unzip the download first.
    echo.
    pause
    exit
)

REM make sure an antivirus hasn't deleted the installer DLL
if not exist %installerDir%"internal\windows\SMAPI.Installer.dll" (
    echo Oops! SMAPI is missing one of its files. Your antivirus might have deleted it.
    echo Missing file: %installerDir%internal\windows\SMAPI.Installer.dll
    echo.
    pause
    exit
)
if not exist %installerDir%"internal\windows\SMAPI.Installer.exe" (
    echo Oops! SMAPI is missing one of its files. Your antivirus might have deleted it.
    echo Missing file: %installerDir%internal\windows\SMAPI.Installer.exe
    echo.
    pause
    exit
)

REM start installer
internal\windows\SMAPI.Installer.exe

REM keep window open if it failed
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Oops! The SMAPI installer seems to have failed. The error details may be shown above.
    echo.
    pause
    exit
)
