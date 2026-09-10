param(
    [string]$projectDir
)

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

Write-Host "projectDir = |$projectDir|"

# Remove all trailing separators
$projectDir = $projectDir.TrimEnd('\')
$projectDir = $projectDir.TrimEnd('/')
$projectDir = $projectDir.TrimEnd('"')

$assetsPath = Join-Path $projectDir "Assets"
$outputPath = Join-Path $projectDir "ZippedAssets"

Write-Host "projectDir = |$projectDir|"
Write-Host "assetsPath = |$assetsPath|"
Write-Host "outputPath = |$outputPath|"

# Create the output folder
if (!(Test-Path $outputPath)) {
    New-Item -ItemType Directory -Path $outputPath | Out-Null
}

$subfolders = Get-ChildItem -Path $assetsPath -Directory

foreach ($folder in $subfolders) {
    $folderPath = $folder.FullName
    $zipName = "$($folder.Name).zip"
    $zipPath = Join-Path $outputPath $zipName
	
	Write-Host "folderPath = |$folderPath|"
	Write-Host "zipName = |$zipName|"
	Write-Host "zipPath = |$zipPath|"

    # Delete existing files
    if (Test-Path $zipPath) {
        Remove-Item $zipPath
    }

    # This uses System.IO.Compression to get proper forward slashes as separators, not windows-exclusive backslashes
	$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)

	try {
		$files = Get-ChildItem -Path $folderPath -Recurse -File

		foreach ($file in $files) {
			$relativePath = $file.FullName.Substring($folderPath.Length + 1)

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

