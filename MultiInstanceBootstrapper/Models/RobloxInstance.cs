namespace MultiInstanceBootstrapper.Models;

public enum InstanceStatus
{
    Stopped,
    Running,
    Starting
}

public class RobloxInstance
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public InstanceStatus Status { get; set; }
    public Process? Process { get; set; }
    public DateTime StartedAt { get; set; }
    public string LaunchArgs { get; set; } = string.Empty;

    public RobloxInstance(int id)
    {
        Id = id;
        Name = $"Instance {id}";
        Status = InstanceStatus.Stopped;
    }
}
