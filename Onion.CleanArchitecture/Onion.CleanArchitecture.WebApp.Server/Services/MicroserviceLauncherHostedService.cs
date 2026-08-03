using System.Diagnostics;

namespace Onion.CleanArchitecture.WebApp.Server.Services;

public class MicroserviceLauncherHostedService : IHostedService
{
    private readonly List<Process> _processes = new();
    private readonly ILogger<MicroserviceLauncherHostedService> _logger;
    private readonly IWebHostEnvironment _env;

    public MicroserviceLauncherHostedService(
        ILogger<MicroserviceLauncherHostedService> logger,
        IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_env.IsDevelopment())
        {
            return Task.CompletedTask;
        }

        var repoRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", ".."));

        var services = new (string Name, string Project, bool UseHttps)[]
        {
            ("OrderSubmitService", Path.Combine(repoRoot, "OrderSubmitService"), false),
            ("OrderOrchestration", Path.Combine(repoRoot, "OrderOrchestration"), false),
            ("OrderAcceptService", Path.Combine(repoRoot, "OrderAcceptService"), false),
            ("OrderCompleteService", Path.Combine(repoRoot, "OrderCompleteService"), false),
            ("NotificationService", Path.Combine(repoRoot, "NotificationService"), true),
        };

        foreach (var (name, project, useHttps) in services)
        {
            if (!Directory.Exists(project))
            {
                _logger.LogWarning("[Launcher] Khong tim thay {Project}", project);
                continue;
            }

            StartService(name, project, useHttps);
        }

        return Task.CompletedTask;
    }

    private void StartService(string name, string project, bool useHttps)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = project,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add(project);
        if (useHttps)
        {
            psi.ArgumentList.Add("--launch-profile");
            psi.ArgumentList.Add("https");
        }

        try
        {
            var proc = Process.Start(psi);
            if (proc == null)
            {
                _logger.LogWarning("[Launcher] Khong the start {Name}", name);
                return;
            }

            proc.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) _logger.LogInformation("[{Name}] {Line}", name, e.Data);
            };
            proc.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) _logger.LogWarning("[{Name}] {Line}", name, e.Data);
            };
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            _processes.Add(proc);
            _logger.LogInformation("[Launcher] Started {Name} (PID {Pid})", name, proc.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Launcher] Khong start duoc {Name}", name);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var proc in _processes)
        {
            try
            {
                if (!proc.HasExited)
                {
                    proc.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // ignore
            }
        }

        _processes.Clear();
        return Task.CompletedTask;
    }
}
