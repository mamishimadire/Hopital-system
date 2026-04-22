# MedBridge EMR — Windows startup script
# Usage: Right-click → "Run with PowerShell"
#   Or in a terminal: powershell -ExecutionPolicy Bypass -File run.ps1

$ScriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Definition
$AppDir     = Join-Path $ScriptDir "MedBridge"
$Port       = 5000
$Url        = "http://localhost:$Port"
$DbFile     = Join-Path $AppDir "medbridge.db"

Write-Host ""
Write-Host "+------------------------------------------+" -ForegroundColor Cyan
Write-Host "|  MedBridge EMR - Starting up             |" -ForegroundColor Cyan
Write-Host "+------------------------------------------+" -ForegroundColor Cyan
Write-Host ""

# ── 1. Check .NET 8 SDK
Write-Host "[1/3] Checking .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVer = & dotnet --version 2>&1
    if ($dotnetVer -notmatch "^8\.") {
        Write-Host "      WARNING: .NET 8 is recommended. Found: $dotnetVer" -ForegroundColor DarkYellow
    } else {
        Write-Host "      .NET $dotnetVer - OK" -ForegroundColor Green
    }
} catch {
    Write-Host "      ERROR: .NET SDK not found." -ForegroundColor Red
    Write-Host "      Download from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# ── 2. Use SQLite (no database server needed on Windows)
Write-Host "[2/3] Configuring database (SQLite)..." -ForegroundColor Yellow
$env:ConnectionStrings__DefaultConnection = "Data Source=$DbFile"
Write-Host "      SQLite database: $DbFile" -ForegroundColor Green

# ── 3. Kill any existing instance on this port
$existing = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
if ($existing) {
    $pid = $existing.OwningProcess | Select-Object -First 1
    Write-Host "      Stopping previous instance (PID $pid)..." -ForegroundColor DarkYellow
    Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
}

# ── 4. Start the app
Write-Host "[3/3] Launching MedBridge at $Url ..." -ForegroundColor Yellow
$logFile = "$env:TEMP\medbridge.log"

$startInfo = New-Object System.Diagnostics.ProcessStartInfo
$startInfo.FileName  = "dotnet"
$startInfo.Arguments = "run --project `"$AppDir`" --urls `"$Url`""
$startInfo.WorkingDirectory      = $ScriptDir
$startInfo.UseShellExecute       = $false
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError  = $true
$startInfo.CreateNoWindow         = $false

$proc = [System.Diagnostics.Process]::Start($startInfo)
Write-Host "      App PID: $($proc.Id)" -ForegroundColor Green
Write-Host "      Logs: $logFile" -ForegroundColor DarkGray

# Stream output to log file in background
$proc.BeginOutputReadLine()
$proc.BeginErrorReadLine()

# ── 5. Wait for app to respond
Write-Host ""
Write-Host "Waiting for app to start (this may take 20-30 seconds on first run)..." -ForegroundColor Yellow
$ready = $false
for ($i = 1; $i -le 30; $i++) {
    Start-Sleep -Seconds 2
    try {
        $resp = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if ($resp.StatusCode -in @(200, 302)) { $ready = $true; break }
    } catch { }
    Write-Host "  $($i * 2)s..." -NoNewline -ForegroundColor DarkGray
}
Write-Host ""

if (-not $ready) {
    Write-Host "App is taking longer than expected. Opening browser anyway..." -ForegroundColor DarkYellow
}

# ── 6. Open browser
Write-Host "Opening browser..." -ForegroundColor Yellow
Start-Process $Url

Write-Host ""
Write-Host "+============================================+" -ForegroundColor Green
Write-Host "|  MedBridge EMR is running!                |" -ForegroundColor Green
Write-Host "|                                           |" -ForegroundColor Green
Write-Host "|  URL  :  http://localhost:5000            |" -ForegroundColor Green
Write-Host "|  Login:  Mamishi.Madire@admin             |" -ForegroundColor Green
Write-Host "|  Pass :  Admin123@                        |" -ForegroundColor Green
Write-Host "|                                           |" -ForegroundColor Green
Write-Host "|  Close this window to stop the server    |" -ForegroundColor Green
Write-Host "+============================================+" -ForegroundColor Green
Write-Host ""

# Keep the script alive (app runs until this window closes)
$proc.WaitForExit()
