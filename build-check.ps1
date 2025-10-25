Write-Host "Building Miller Craft Tools project..."
try {
    # Try to find MSBuild path
    $vsPath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019"
    $msBuildPath = Get-ChildItem -Path $vsPath -Recurse -Filter "MSBuild.exe" | Select-Object -First 1
    
    if ($msBuildPath) {
        Write-Host "Found MSBuild at: $($msBuildPath.FullName)"
        & $msBuildPath.FullName "Miller Craft Tools.sln" /p:Configuration=Debug /v:m
    } else {
        # Try using vswhere to find latest MSBuild
        $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
        if (Test-Path $vswhere) {
            $msBuildPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
            if ($msBuildPath) {
                Write-Host "Found MSBuild at: $msBuildPath"
                & $msBuildPath "Miller Craft Tools.sln" /p:Configuration=Debug /v:m
            }
        }
    }
    
    # If MSBuild not found, try dotnet build
    if (!$msBuildPath) {
        Write-Host "MSBuild not found, trying dotnet build..."
        dotnet build "Miller Craft Tools.sln" --configuration Debug
    }
} catch {
    Write-Host "Build error: $_"
    exit 1
}

# Check for error files to verify if build succeeded
if (Test-Path ".\bin\Debug\*.dll") {
    Write-Host "Build appears successful! DLLs were created."
} else {
    Write-Host "Build may have failed. No DLLs found in output directory."
}
