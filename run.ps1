# MedBridge EMR — Windows startup script
# Usage: powershell -ExecutionPolicy Bypass -File run.ps1

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$AppDir    = Join-Path $ScriptDir "MedBridge"
$Url       = "http://localhost:5000"

Write-Host ""
Write-Host "+------------------------------------------+" -ForegroundColor Cyan
Write-Host "|  MedBridge EMR - Starting                |" -ForegroundColor Cyan
Write-Host "+------------------------------------------+" -ForegroundColor Cyan
Write-Host ""

# Check .NET SDK
try {
    $v = & dotnet --version 2>&1
    Write-Host "  .NET SDK: $v" -ForegroundColor Green
} catch {
    Write-Host "  ERROR: .NET SDK not found." -ForegroundColor Red
    Write-Host "  Download from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Use SQL Server LocalDB — change this connection string when you get the company database
$env:ConnectionStrings__DefaultConnection = "Server=(localdb)\MSSQLLocalDB;Database=MedBridgeEMR;Trusted_Connection=True;TrustServerCertificate=True"
Write-Host "  Database : SQL Server LocalDB (MedBridgeEMR)" -ForegroundColor Green
Write-Host "  URL      : $Url" -ForegroundColor Green
Write-Host ""
Write-Host "Starting app... (first run takes 30-60 seconds to compile)" -ForegroundColor Yellow
Write-Host "Browser will open automatically. Keep this window open." -ForegroundColor Yellow
Write-Host "----------------------------------------------" -ForegroundColor DarkGray
Write-Host ""

# Open browser after 30 seconds in background
$job = Start-Job -ScriptBlock {
    param($url)
    Start-Sleep -Seconds 30
    Start-Process $url
} -ArgumentList $Url

# Run app directly — output is visible so you can see any errors
dotnet run --project "$AppDir" --urls "$Url"

# Cleanup background job
Stop-Job $job -ErrorAction SilentlyContinue
Remove-Job $job -ErrorAction SilentlyContinue
