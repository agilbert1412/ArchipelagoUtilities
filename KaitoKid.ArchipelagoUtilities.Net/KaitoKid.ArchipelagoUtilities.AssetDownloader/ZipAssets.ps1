param(
    [string]$projectDir
)

# Normalize trailing slash
$projectDir = $projectDir.TrimEnd('\')

# Path to the Assets folder, relative or absolute
$assetsPath = Join-Path $projectDir "Assets"

# Path where the ZIP files should be written
$outputPath = Join-Path $projectDir "ZippedAssets"

# Ensure the output folder exists
if (!(Test-Path $outputPath)) {
    New-Item -ItemType Directory -Path $outputPath | Out-Null
}

# Get all immediate subfolders of Assets
$subfolders = Get-ChildItem -Path $assetsPath -Directory

foreach ($folder in $subfolders) {
    $folderPath = $folder.FullName
    $zipName = "$($folder.Name).zip"
    $zipPath = Join-Path $outputPath $zipName

    # If a ZIP already exists, delete it (optional but recommended)
    if (Test-Path $zipPath) {
        Remove-Item $zipPath
    }

    # Create ZIP
    Compress-Archive -Path $folderPath -DestinationPath $zipPath
}
