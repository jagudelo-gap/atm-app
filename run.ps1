#Requires -Version 7
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

if (-not (Test-Path .env)) {
    Copy-Item .env.example .env
    Write-Host 'Created .env from .env.example'
}

$webPort = '3000'
$line = Select-String -Path .env -Pattern '^\s*WEB_PORT\s*=\s*(\d+)' | Select-Object -First 1
if ($line) { $webPort = $line.Matches[0].Groups[1].Value }
$url = "http://localhost:$webPort"

docker compose up --build -d
if ($LASTEXITCODE -ne 0) { throw 'docker compose up failed' }

Write-Host "Waiting for $url ..."
$deadline = (Get-Date).AddMinutes(2)
while ((Get-Date) -lt $deadline) {
    try {
        $r = Invoke-WebRequest -Uri "$url/health" -UseBasicParsing -TimeoutSec 5
        if ($r.StatusCode -eq 200) {
            Write-Host "ATM is ready: $url"
            return
        }
    } catch { }
    Start-Sleep -Seconds 2
}
docker compose ps
throw "Timed out waiting for $url"
