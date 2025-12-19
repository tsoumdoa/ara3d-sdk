@echo off
setlocal

REM Source directory = folder of the built project
set SRC_DIR=%~dp0

REM Target directory relative to project folder
set DEST_DIR=%SRC_DIR%..\..\..\..\bim-open-schema\spec

echo Copying BIM Open Schema source files...
echo From: %SRC_DIR%
echo To:   %DEST_DIR%
echo.

REM Ensure destination exists
if not exist "%DEST_DIR%" (
    echo Creating destination directory...
    mkdir "%DEST_DIR%"
)

REM Copy files (overwrite silently)
copy /Y "%SRC_DIR%BimGeometry.cs" "%DEST_DIR%\BimGeometry.cs"
copy /Y "%SRC_DIR%BimObjectModel.cs" "%DEST_DIR%\BimOpenSchema.cs"

echo Done.
endlocal
