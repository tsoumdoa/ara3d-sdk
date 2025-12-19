
    
set AddinsDir=%AppData%\Autodesk\Revit\Addins
set BosDir=%AddinsDir%\2025\Ara3D.BIMOpenSchema
set AddinName=BIMOpenSchema.addin

:: -------- 1) No argument?  Leave quietly --------------------
if "%~1"=="" (
    echo No argument supplied – nothing to do.
    goto :eof
)

:: -------- 2)  -clean  ---------------------------------------
if /I "%~1"=="-clean" goto :clean

:: -------- 3)  Normal install --------------------------------

rd /S /Q "%1\runtimes\linux-arm64"
rd /S /Q "%1\runtimes\linux-x64"
rd /S /Q "%1\runtimes\osx-arm64"

if not exist "%BosDir%" mkdir "%BosDir%"
xcopy /Y %AddinName% "%AddinsDir%\2025"
xcopy %1 "%BosDir%" /i /c /k /y

echo Done.
goto :eof

:clean
echo Removing BIM Open Schema for Revit 2025 …

REM Delete manifest(s) we previously copied
if exist "%BosDir%" (
    del /Q "%BosDir%\..\%AddinName%" >nul 2>&1
)

REM Remove add-in folder and scripts folder (with contents)
if exist "%BosDir%" rd /S /Q "%BosDir%"

echo Clean-up complete.
goto :eof