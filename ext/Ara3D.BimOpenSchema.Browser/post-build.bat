set BimOpenSchemaDir=%AppData%\Autodesk\Revit\Addins\2025\Ara3D.BIMOpenSchema\
if not exist "%BimOpenSchemaDir%" mkdir "%BimOpenSchemaDir%"
xcopy %1 "%BimOpenSchemaDir%" /i /c /k /y

