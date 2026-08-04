# stop-all.ps1 - Dung tat ca service duoc start boi run-all.ps1

$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$pidsFile = Join-Path $root ".run-logs\pids.txt"

if (-not (Test-Path $pidsFile)) {
    Write-Host "Khong tim thay pids.txt. Cac service co the chua duoc start bang run-all.ps1."
    exit 0
}

foreach ($line in Get-Content $pidsFile) {
    $parts = $line.Split("=")
    if ($parts.Count -eq 2) {
        $name = $parts[0]
        $pid = [int]$parts[1]
        try {
            Stop-Process -Id $pid -Force -ErrorAction Stop
            Write-Host ("Stopped {0} (PID {1})" -f $name, $pid)
        }
        catch {
            Write-Host ("{0}: da khong con chay hoac khong the dung (PID {1})" -f $name, $pid)
        }
    }
}

Remove-Item $pidsFile -ErrorAction SilentlyContinue
Write-Host "Done."
