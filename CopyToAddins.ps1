# Define paths
$SourceDirRelease = "C:\Users\jeff\source\repos\Miller Craft Tools\bin\Release"
$SourceDirDebug = "C:\Users\jeff\source\repos\Miller Craft Tools\bin\Debug"
$ProjectRoot = "C:\Users\jeff\source\repos\Miller Craft Tools"
$TargetDir = "C:\Users\jeff\AppData\Roaming\Autodesk\Revit\Addins\2025"
$TargetDir2 = "C:\Users\jeff\AppData\Roaming\Autodesk\Revit\Addins\2026"

Write-Host "Copying compiled files to Revit Addins directories..."

# Determine the source directory (check Release first, then Debug)
$SourceDir = $SourceDirRelease
$DllSource = Join-Path $SourceDir "Miller_Craft_Tools.dll"
if (-not (Test-Path $DllSource)) {
    Write-Host "DLL not found in Release directory. Checking Debug directory..."
    $SourceDir = $SourceDirDebug
    $DllSource = Join-Path $SourceDir "Miller_Craft_Tools.dll"
}

# Ensure first target directory exists (Revit 2025)
if (-not (Test-Path $TargetDir)) {
    Write-Host "Target directory does not exist. Creating $TargetDir..."
    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
}

# Ensure second target directory exists (Revit 2026)
if (-not (Test-Path $TargetDir2)) {
    Write-Host "Target directory does not exist. Creating $TargetDir2..."
    New-Item -ItemType Directory -Path $TargetDir2 -Force | Out-Null
}

# List all files in the build output for debugging
Write-Host "Files in build output directory ($SourceDir):"
Get-ChildItem -Path $SourceDir -ErrorAction SilentlyContinue | ForEach-Object { Write-Host " - $($_.Name)" }

# Copy the DLL to both target directories
if (Test-Path $DllSource) {
    try {
        # Copy to Revit 2025
        Copy-Item -Path $DllSource -Destination $TargetDir -Force
        Write-Host "Successfully copied Miller_Craft_Tools.dll to $TargetDir"

        # Copy to Revit 2026
        Copy-Item -Path $DllSource -Destination $TargetDir2 -Force
        Write-Host "Successfully copied Miller_Craft_Tools.dll to $TargetDir2"
    }
    catch {
        Write-Host "Failed to copy Miller_Craft_Tools.dll: $($_.Exception.Message)"
        Write-Host "StackTrace: $($_.Exception.StackTrace)"
        exit 1
    }
}
else {
    Write-Host "Miller_Craft_Tools.dll not found in $SourceDir"
    exit 1
}

# Copy the .addin file from the project root to both target directories
$AddinSource = Join-Path $ProjectRoot "Miller_Craft_Tools.addin"
if (Test-Path $AddinSource) {
    try {
        # Copy to Revit 2025
        Copy-Item -Path $AddinSource -Destination $TargetDir -Force
        Write-Host "Successfully copied Miller_Craft_Tools.addin to $TargetDir"

        # Copy to Revit 2026
        Copy-Item -Path $AddinSource -Destination $TargetDir2 -Force
        Write-Host "Successfully copied Miller_Craft_Tools.addin to $TargetDir2"
    }
    catch {
        Write-Host "Failed to copy Miller_Craft_Tools.addin: $($_.Exception.Message)"
        Write-Host "StackTrace: $($_.Exception.StackTrace)"
        exit 1
    }
}
else {
    Write-Host "Miller_Craft_Tools.addin not found in $ProjectRoot"
    exit 1
}

# Verify the contents of the first target directory (Revit 2025)
Write-Host "Files in target directory ($TargetDir):"
Get-ChildItem -Path $TargetDir -ErrorAction SilentlyContinue | ForEach-Object { Write-Host " - $($_.Name)" }

# Verify the contents of the second target directory (Revit 2026)
Write-Host "Files in target directory ($TargetDir2):"
Get-ChildItem -Path $TargetDir2 -ErrorAction SilentlyContinue | ForEach-Object { Write-Host " - $($_.Name)" }

Write-Host "Done!"
exit 0