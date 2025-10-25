# Define paths
$SourceDirRelease = "C:\Users\jeff\source\repos\Miller Craft Tools\bin\x64\Release"
$SourceDirDebug = "C:\Users\jeff\source\repos\Miller Craft Tools\bin\x64\Publish"
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

# Prepare subfolder for DLLs and Resources
$Subfolder = "Miller_Craft_Assistant"
$TargetSubDir2025 = Join-Path $TargetDir $Subfolder
$TargetSubDir2026 = Join-Path $TargetDir2 $Subfolder

# Ensure subfolders exist
if (-not (Test-Path $TargetSubDir2025)) { New-Item -ItemType Directory -Path $TargetSubDir2025 -Force | Out-Null }
if (-not (Test-Path $TargetSubDir2026)) { New-Item -ItemType Directory -Path $TargetSubDir2026 -Force | Out-Null }

# Copy all DLLs to subfolders
Write-Host "Copying DLLs to Miller_Craft_Assistant subfolders..."
$dllCount = 0
Get-ChildItem -Path $SourceDir -Filter *.dll | ForEach-Object {
    $dllCount++
    Write-Host "  Copying $($_.Name)..."
    Copy-Item $_.FullName -Destination $TargetSubDir2025 -Force
    Copy-Item $_.FullName -Destination $TargetSubDir2026 -Force
    Write-Host "    -> Successfully copied to both 2025 and 2026"
}
if ($dllCount -eq 0) {
    Write-Host "WARNING: No DLL files found in $SourceDir"
} else {
    Write-Host "Successfully copied $dllCount DLL file(s) to Miller_Craft_Assistant subfolders"
}

# Copy Resources folder to subfolders
$ResourcesSource = Join-Path $ProjectRoot "Resources"
if (Test-Path $ResourcesSource) {
    Copy-Item $ResourcesSource -Destination $TargetSubDir2025 -Recurse -Force
    Copy-Item $ResourcesSource -Destination $TargetSubDir2026 -Recurse -Force
}

# Remove any DLLs from the root Addins folders (cleanup)
Get-ChildItem -Path $TargetDir -Filter *.dll -File | Remove-Item -Force -ErrorAction SilentlyContinue
Get-ChildItem -Path $TargetDir2 -Filter *.dll -File | Remove-Item -Force -ErrorAction SilentlyContinue

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

# Copy the Resources folder and its contents to both target directories
$ResourcesSource = Join-Path $ProjectRoot "Resources"
if (Test-Path $ResourcesSource) {
    try {
        $ResourcesTarget1 = Join-Path $TargetDir "Resources"
        $ResourcesTarget2 = Join-Path $TargetDir2 "Resources"
        # Remove existing Resources folder if present
        if (Test-Path $ResourcesTarget1) { Remove-Item $ResourcesTarget1 -Recurse -Force }
        if (Test-Path $ResourcesTarget2) { Remove-Item $ResourcesTarget2 -Recurse -Force }
        # Copy recursively
        Copy-Item -Path $ResourcesSource -Destination $ResourcesTarget1 -Recurse
        Write-Host "Successfully copied Resources folder to $ResourcesTarget1"
        Copy-Item -Path $ResourcesSource -Destination $ResourcesTarget2 -Recurse
        Write-Host "Successfully copied Resources folder to $ResourcesTarget2"
    } catch {
        Write-Host "Failed to copy Resources folder: $($_.Exception.Message)"
        Write-Host "StackTrace: $($_.Exception.StackTrace)"
    }
} else {
    Write-Host "Resources folder not found in $ProjectRoot. Skipping Resources copy."
}

# Verify the contents of the first target directory (Revit 2025)
Write-Host "Files in target directory ($TargetDir):"
Get-ChildItem -Path $TargetDir -ErrorAction SilentlyContinue | ForEach-Object { Write-Host " - $($_.Name)" }

# Verify the contents of the second target directory (Revit 2026)
Write-Host "Files in target directory ($TargetDir2):"
Get-ChildItem -Path $TargetDir2 -ErrorAction SilentlyContinue | ForEach-Object { Write-Host " - $($_.Name)" }

# Verify the Miller_Craft_Tools.dll was copied correctly
Write-Host ""
Write-Host "Verifying Miller_Craft_Tools.dll deployment:"
$dll2026 = Join-Path $TargetSubDir2026 "Miller_Craft_Tools.dll"
if (Test-Path $dll2026) {
    $dllInfo = Get-Item $dll2026
    Write-Host "  2026 DLL: Found at $dll2026"
    Write-Host "  Last Modified: $($dllInfo.LastWriteTime)"
    Write-Host "  Size: $($dllInfo.Length) bytes"
} else {
    Write-Host "  ERROR: Miller_Craft_Tools.dll NOT FOUND in $TargetSubDir2026"
}

Write-Host ""
Write-Host "Done!"
exit 0