param(
    [string]$FramesDir = "logs/platec_frames",
    [string]$Output = "logs/platec.mp4",
    [int]$Framerate = 30
)

# Resolve paths
$FramesDir = [System.IO.Path]::GetFullPath($FramesDir)
$Output = [System.IO.Path]::GetFullPath($Output)

if (-not (Test-Path $FramesDir)) {
    Write-Error "Frames directory not found: $FramesDir"
    exit 1
}

# Find a seed/size subfolder if top-level dir contains multiple runs
$subdirs = Get-ChildItem -Directory -Path $FramesDir -ErrorAction SilentlyContinue
if ($subdirs.Count -gt 0) {
    # Pick the most recently written subdir by default
    $FramesDir = ($subdirs | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
}

# Verify frames exist
$frames = Get-ChildItem -Path $FramesDir -Filter "frame_*.pgm" -ErrorAction SilentlyContinue
if ($frames.Count -eq 0) {
    Write-Error "No frames found in $FramesDir (expected frame_0001.pgm etc.)"
    exit 1
}

# Ensure output directory exists
$null = New-Item -ItemType Directory -Path ([System.IO.Path]::GetDirectoryName($Output)) -Force -ErrorAction SilentlyContinue

# Build ffmpeg command
$ffmpegArgs = @(
    '-y',
    '-framerate', $Framerate,
    '-i', 'frame_%04d.pgm',
    '-c:v', 'libx264',
    '-pix_fmt', 'yuv420p',
    '-movflags', '+faststart',
    ("`"$Output`"")
)

Push-Location $FramesDir
try {
    Write-Host "Encoding video from frames in $FramesDir -> $Output at ${Framerate}fps"
    $proc = Start-Process -FilePath ffmpeg -ArgumentList $ffmpegArgs -NoNewWindow -Wait -PassThru -ErrorAction Stop
    if ($proc.ExitCode -ne 0) {
        Write-Error "ffmpeg exited with code $($proc.ExitCode)"
        exit $proc.ExitCode
    }
    Write-Host "Done. Output: $Output"
}
finally {
    Pop-Location
}
