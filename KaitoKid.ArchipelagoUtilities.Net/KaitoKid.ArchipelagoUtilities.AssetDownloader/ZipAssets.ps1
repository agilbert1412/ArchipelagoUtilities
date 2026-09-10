param(
    [string]$projectDir
)

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

Write-Host "projectDir = |$projectDir|"

# Normalize trailing slash
$projectDir = $projectDir.TrimEnd('\')
$projectDir = $projectDir.TrimEnd('/')
$projectDir = $projectDir.TrimEnd('"')

# Path to the Assets folder, relative or absolute
$assetsPath = Join-Path $projectDir "Assets"

# Path where the ZIP files should be written
$outputPath = Join-Path $projectDir "ZippedAssets"

Write-Host "projectDir = |$projectDir|"
Write-Host "assetsPath = |$assetsPath|"
Write-Host "outputPath = |$outputPath|"

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
	
	Write-Host "folderPath = |$folderPath|"
	Write-Host "zipName = |$zipName|"
	Write-Host "zipPath = |$zipPath|"

    # If a ZIP already exists, delete it (optional but recommended)
    if (Test-Path $zipPath) {
        Remove-Item $zipPath
    }

    # Create ZIP with POSIX-style separators
	$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)

	try {
		$files = Get-ChildItem -Path $folderPath -Recurse -File

		foreach ($file in $files) {
			# Relative path inside the zip
			$relativePath = $file.FullName.Substring($folderPath.Length + 1)

			# ZIP format expects forward slashes
			$entryName = $relativePath.Replace('\', '/')
			$entryName = "$($folder.Name)/$($entryName)"

			Write-Host "file = |$file|"
			Write-Host "file.FullName = |$file.FullName|"
			Write-Host "relativePath = |$relativePath|"
			Write-Host "entryName = |$entryName|"

			[System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
				$zip,
				$file.FullName,
				$entryName,
				[System.IO.Compression.CompressionLevel]::Optimal
			) | Out-Null
		}
	}
	finally {
		$zip.Dispose()
	}
}

