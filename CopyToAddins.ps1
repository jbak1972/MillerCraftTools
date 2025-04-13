# Define paths
$SourceDir = "C:\Users\jeff\source\repos\Miller Craft Tools\bin\Release"
$ProjectRoot = "C:\Users\jeff\source\repos\Miller Craft Tools"
$TargetDir = "C:\Users\jeff\AppData\Roaming\Autodesk\Revit\Addins\2025"

Write-Host "Copying compiled files to Revit Addins directory..."

# Ensure target directory exists
if (-not (Test-Path $TargetDir)) {
    Write-Host "Target directory does not exist. Creating $TargetDir..."
    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
}

# List all files in the build output for debugging
Write-Host "Files in build output directory ($SourceDir):"
Get-ChildItem -Path $SourceDir | ForEach-Object { Write-Host " - $($_.Name)" }

# Copy the DLL
$DllSource = Join-Path $SourceDir "Miller_Craft_Tools.dll"
if (Test-Path $DllSource) {
    try {
        Copy-Item -Path $DllSource -Destination $TargetDir -Force
        Write-Host "Successfully copied Miller_Craft_Tools.dll"
    }
    catch {
        Write-Host "Failed to copy Miller_Craft_Tools.dll: $_"
        exit 1
    }
}
else {
    Write-Host "Miller_Craft_Tools.dll not found in $SourceDir"
    exit 1
}

# Copy the .addin file from the project root
$AddinSource = Join-Path $ProjectRoot "Miller_Craft_Tools.addin"
if (Test-Path $AddinSource) {
    try {
        Copy-Item -Path $AddinSource -Destination $TargetDir -Force
        Write-Host "Successfully copied Miller_Craft_Tools.addin"
    }
    catch {
        Write-Host "Failed to copy Miller_Craft_Tools.addin: $_"
        exit 1
    }
}
else {
    Write-Host "Miller_Craft_Tools.addin not found in $ProjectRoot"
    exit 1
}

# Verify the target directory contents
Write-Host "Files in target directory ($TargetDir):"
Get-ChildItem -Path $TargetDir | ForEach-Object { Write-Host " - $($_.Name)" }

Write-Host "Done!"
exit 0