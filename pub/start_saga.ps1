$proc = Start-Process -FilePath "C:\Program Files\dotnet\dotnet.exe" `
  -ArgumentList @("run", "--no-build") `
  -WorkingDirectory "D:\Univer\Nam_4\VB\Demo-Saga\OrderOrchestration" `
  -RedirectStandardOutput "D:\Univer\Nam_4\VB\Demo-Saga\pub\saga.out.log" `
  -RedirectStandardError "D:\Univer\Nam_4\VB\Demo-Saga\pub\saga.err.log" `
  -PassThru -NoNewWindow
Write-Output ("Started PID=" + $proc.Id)
