using System.Collections.ObjectModel;
using System.Diagnostics;
using MultiInstanceBootstrapper.Helpers;
using MultiInstanceBootstrapper.Models;

namespace MultiInstanceBootstrapper.Services;

public class InstanceService
{
    private readonly RobloxService _robloxService;
    private readonly ObservableCollection<RobloxInstance> _instances;
    private readonly object _lock = new();

    public ObservableCollection<RobloxInstance> Instances => _instances;
    public int ActiveCount { get; private set; }
    public bool CanLaunch => ActiveCount < Constants.MaxInstances;

    public InstanceService()
    {
        _robloxService = new RobloxService();
        _instances = new ObservableCollection<RobloxInstance>();
        ActiveCount = 0;
    }

    public bool CanLaunchInstance()
    {
        lock (_lock)
        {
            return ActiveCount < Constants.MaxInstances && _robloxService.IsRobloxInstalled();
        }
    }

    public async Task<RobloxInstance?> LaunchInstanceAsync()
    {
        lock (_lock)
        {
            if (ActiveCount >= Constants.MaxInstances)
                return null;
            if (!_robloxService.IsRobloxInstalled())
                return null;
        }

        var robloxPath = _robloxService.GetRobloxPath();
        if (string.IsNullOrEmpty(robloxPath))
            return null;

        var id = ActiveCount + 1;
        var instance = new RobloxInstance(id);
        instance.Status = InstanceStatus.Starting;
        instance.LaunchArgs = $"-instance {id} -id {id}";

        _instances.Add(instance);

        var process = _robloxService.LaunchInstance(robloxPath, id);
        if (process != null)
        {
            instance.Process = process;
            instance.Status = InstanceStatus.Running;
            instance.StartedAt = DateTime.Now;
            ActiveCount++;
        }
        else
        {
            instance.Status = InstanceStatus.Stopped;
            _instances.Remove(instance);
        }

        return instance;
    }

    public bool KillInstance(int instanceId)
    {
        lock (_lock)
        {
            var instance = _instances.FirstOrDefault(i => i.Id == instanceId);
            if (instance == null) return false;

            if (instance.Process != null && !instance.Process.HasExited)
            {
                _robloxService.KillInstance(instance.Process);
            }

            instance.Status = InstanceStatus.Stopped;
            _instances.Remove(instance);
            ActiveCount = Math.Max(0, ActiveCount - 1);
            return true;
        }
    }

    public void KillAll()
    {
        lock (_lock)
        {
            var toRemove = _instances.Where(i => i.Status == InstanceStatus.Running || i.Status == InstanceStatus.Starting).ToList();
            foreach (var instance in toRemove)
            {
                if (instance.Process != null && !instance.Process.HasExited)
                {
                    _robloxService.KillInstance(instance.Process);
                }
                instance.Status = InstanceStatus.Stopped;
                _instances.Remove(instance);
            }
            ActiveCount = 0;
        }
    }

    public void Cleanup()
    {
        lock (_lock)
        {
            var running = _instances.Where(i => i.Process != null && !i.Process.HasExited).ToList();
            foreach (var instance in running)
            {
                instance.Status = InstanceStatus.Stopped;
                instance.Process?.Dispose();
                _instances.Remove(instance);
            }
            ActiveCount = 0;
        }
    }
}
