@echo off
set SOURCE_DIR=C:\Users\jeff\AppData\Roaming\Autodesk\Revit\Addins\2025\Release\net8.0-windows
set TARGET_DIR=C:\Users\jeff\AppData\Roaming\Autodesk\Revit\Addins\2025

echo Copying compiled files to Revit Addins directory...

:: Copy the DLL, overwriting if it exists
if exist "%SOURCE_DIR%\Miller_Craft_Tools.dll" (
    copy /Y "%SOURCE_DIR%\Miller_Craft_Tools.dll" "%TARGET_DIR%\Miller_Craft_Tools.dll"
    if %ERRORLEVEL%==0 (
        echo Successfully copied Miller_Craft_Tools.dll
    ) else (
        echo Failed to copy Miller_Craft_Tools.dll
        exit /b 1
    )
) else (
    echo Miller_Craft_Tools.dll not found in %SOURCE_DIR%
    exit /b 1
)

:: Copy the .addin file, overwriting if it exists
if exist "%SOURCE_DIR%\Miller_Craft_Tools.addin" (
    copy /Y "%SOURCE_DIR%\Miller_Craft_Tools.addin" "%TARGET_DIR%\Miller_Craft_Tools.addin"
    if %ERRORLEVEL%==0 (
        echo Successfully copied Miller_Craft_Tools.addin
    ) else (
        echo Failed to copy Miller_Craft_Tools.addin
        exit /b 1
    )
) else (
    echo Miller_Craft_Tools.addin not found in %SOURCE_DIR%. If this file is not in the build output, ensure it’s copied there first.
)

echo Done!
exit /b 0