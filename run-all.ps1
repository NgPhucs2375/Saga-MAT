# run-all.ps1 - Chay tat ca cac service cua Demo-Saga bang 1 lenh
# Cach dung: .\run-all.ps1   (hoac powershell -ExecutionPolicy Bypass -File .\run-all.ps1)

$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$logDir = Join-Path $root ".run-logs"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

$services = @(
    @{ Name = "OrderSubmitService";    Project = (Join-Path $root "OrderSubmitService");    Args = @("run", "--project", (Join-Path $root "OrderSubmitService")) },
    @{ Name = "OrderOrchestration";    Project = (Join-Path $root "OrderOrchestration");    Args = @("run", "--project", (Join-Path $root "OrderOrchestration")) },
    @{ Name = "OrderAcceptService";    Project = (Join-Path $root "OrderAcceptService");    Args = @("run", "--project", (Join-Path $root "OrderAcceptService")) },
    @{ Name = "OrderCompleteService";  Project = (Join-Path $root "OrderCompleteService");  Args = @("run", "--project", (Join-Path $root "OrderCompleteService")) },
    @{ Name = "ApproveOrderService";   Project = (Join-Path $root "ApproveOrderService");   Args = @("run", "--project", (Join-Path $root "ApproveOrderService")) },
    @{ Name = "SMSService";            Project = (Join-Path $root "SMSService");            Args = @("run", "--project", (Join-Path $root "SMSService")) },
    @{ Name = "WebApp.Server";         Project = (Join-Path $root "Onion.CleanArchitecture\Onion.CleanArchitecture.WebApp.Server"); Args = @("run", "--project", (Join-Path $root "Onion.CleanArchitecture\Onion.CleanArchitecture.WebApp.Server"), "--launch-profile", "https") }
)

$pids = @()
foreach ($svc in $services) {
    $out = Join-Path $logDir ($svc.Name + ".out.log")
    $err = Join-Path $logDir ($svc.Name + ".err.log")
    Write-Host ("Starting {0} ..." -f $svc.Name)
    $p = Start-Process -FilePath "dotnet" `
        -ArgumentList $svc.Args `
        -WorkingDirectory $svc.Project `
        -RedirectStandardOutput $out `
        -RedirectStandardError $err `
        -PassThru `
        -NoNewWindow
    $pids += , @($svc.Name, $p.Id)
}

$pids | ForEach-Object { "{0}={1}" -f $_[0], $_[1] } | Set-Content (Join-Path $logDir "pids.txt")

Write-Host ""
Write-Host ("Tat ca {0} service da duoc start. Log trong: {1}" -f $pids.Count, $logDir)
Write-Host "Mo FE tai: https://localhost:7058"
Write-Host "De dung tat ca: .\stop-all.ps1"
