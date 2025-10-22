    
set AddinsDir=%programdata%\Autodesk\Revit\Addins\
set BowerbirdDir=%AddinsDir%\2025\Ara3D.Bowerbird\
set ScriptsDir=%localappdata%\Ara 3D\Bowerbird for Revit 2025\Scripts\

if "%~1"=="" (
    echo No argument supplied – nothing to do.
    goto :eof
)

if not exist "%BowerbirdDir%" mkdir "%BowerbirdDir%"
xcopy %1 "%BowerbirdDir%" /h /i /c /k /e /r /y
mkdir "%ScriptsDir%"
del "%ScriptsDir%"\*.* /y
xcopy ..\Ara3D.Bowerbird.RevitSamples\*.cs "%ScriptsDir%" /y
xcopy ..\Ara3D.Bowerbird.RevitSamples\*.txt "%ScriptsDir%" /y


echo Done.
