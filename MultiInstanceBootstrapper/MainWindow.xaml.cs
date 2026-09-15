using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MultiInstanceBootstrapper.Models;
using MultiInstanceBootstrapper.Services;
using MultiInstanceBootstrapper.Helpers;

namespace MultiInstanceBootstrapper;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly InstanceService _instanceService;
    private readonly UpdateService _updateService;
    private readonly RobloxService _robloxService;
    private string _displayName = "Loading...";
    private string _avatarUrl = "https://images.roblox.com/headshot?userId=0";
    private string _activeCountText = "0/3";
    private string _versionText = "Version 1.2.0";
    private string _statusText = "Ready";
    private string _updateStatusText = "";
    private string _emptyMessage = "No instances running. Click Launch Instance to get started.";

    public ObservableCollection<RobloxInstanceViewModel> Instances { get; set; } = new();

    public string DisplayName { get => _displayName; set { _displayName = value; OnPropertyChanged(); } }
    public string AvatarUrl { get => _avatarUrl; set { _avatarUrl = value; OnPropertyChanged(); } }
    public string ActiveCountText { get => _activeCountText; set { _activeCountText = value; OnPropertyChanged(); } }
    public string VersionText { get => _versionText; set { _versionText = value; OnPropertyChanged(); } }
    public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }
    public string UpdateStatusText { get => _updateStatusText; set { _updateStatusText = value; OnPropertyChanged(); } }
    public string EmptyMessage { get => _emptyMessage; set { _emptyMessage = value; OnPropertyChanged(); } }

    public ICommand LaunchCommand { get; }
    public ICommand KillAllCommand { get; }
    public ICommand CheckUpdateCommand { get; }

    public MainWindowViewModel()
    {
        _instanceService = new InstanceService();
        _updateService = new UpdateService(Constants.AppVersion);
        _robloxService = new RobloxService();

        LaunchCommand = new RelayCommand(async _ => await LaunchInstance(), _ => _instanceService.CanLaunchInstance());
        KillAllCommand = new RelayCommand(_ => KillAllInstances(), _ => Instances.Count > 0);
        CheckUpdateCommand = new RelayCommand(async _ => await CheckForUpdates());

        LoadUserProfile();
        UpdateEmptyMessage();
        StartUpdateChecker();
    }

    private async void LoadUserProfile()
    {
        try
        {
            if (_robloxService.IsRobloxInstalled())
            {
                DisplayName = "Roblox User";
                StatusText = "Ready";
            }
            else
            {
                DisplayName = "Roblox Not Found";
                StatusText = "Roblox not detected. Please install Roblox.";
            }
        }
        catch
        {
            DisplayName = "Roblox User";
        }
    }

    private async System.Threading.Tasks.Task LaunchInstance()
    {
        StatusText = "Launching instance...";
        var instance = await _instanceService.LaunchInstanceAsync();
        if (instance != null)
        {
            var vm = new RobloxInstanceViewModel(instance);
            Instances.Add(vm);
            UpdateCounts();
            UpdateEmptyMessage();
            StatusText = $"Instance {instance.Id} launched!";
        }
        else
        {
            StatusText = "Failed to launch instance.";
        }
        ((RelayCommand)LaunchCommand).RaiseCanExecuteChanged();
        ((RelayCommand)KillAllCommand).RaiseCanExecuteChanged();
    }

    private void KillAllInstances()
    {
        _instanceService.KillAll();
        Instances.Clear();
        UpdateCounts();
        UpdateEmptyMessage();
        StatusText = "All instances killed.";
        ((RelayCommand)LaunchCommand).RaiseCanExecuteChanged();
        ((RelayCommand)KillAllCommand).RaiseCanExecuteChanged();
    }

    private async System.Threading.Tasks.Task CheckForUpdates()
    {
        UpdateStatusText = "Checking...";
        try
        {
            var update = await _updateService.CheckForUpdatesAsync();
            if (update.HasUpdate)
            {
                UpdateStatusText = $"Update v{update.Version} available!";
                var result = MessageBox.Show($"Update v{update.Version} is available!\n\nDownload now?", "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                {
                    var path = await _updateService.DownloadLatestReleaseAsync();
                    if (path != null)
                    {
                        UpdateStatusText = "Update downloaded! Restarting...";
                        // Would restart here
                    }
                    else
                    {
                        UpdateStatusText = "Download failed.";
                    }
                }
            }
            else
            {
                UpdateStatusText = "Up to date";
            }
        }
        catch
        {
            UpdateStatusText = "Update check failed";
        }
    }

    private void UpdateCounts()
    {
        var count = Instances.Count(i => i.Status == InstanceStatus.Running || i.Status == InstanceStatus.Starting);
        ActiveCountText = $"{count}/{Constants.MaxInstances}";
        ((RelayCommand)KillAllCommand).RaiseCanExecuteChanged();
    }

    private void UpdateEmptyMessage()
    {
        EmptyMessage = Instances.Count == 0
            ? "No instances running. Click Launch Instance to get started."
            : "";
    }

    private void StartUpdateChecker()
    {
        var timer = new System.Timers.Timer(Constants.UpdateCheckIntervalMs);
        timer.Elapsed += async (s, e) => await CheckForUpdates();
        timer.AutoReset = true;
        timer.Enabled = true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class RobloxInstanceViewModel : INotifyPropertyChanged
{
    private readonly RobloxInstance _instance;
    private string _statusText;
    private string _buttonText;
    private string _detailText;
    private string _statusColor;

    public RobloxInstanceViewModel(RobloxInstance instance)
    {
        _instance = instance;
        UpdateDisplay();
    }

    public string Name => _instance.Name;
    public string LaunchArgs => _instance.LaunchArgs;

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public string ButtonText
    {
        get => _buttonText;
        set { _buttonText = value; OnPropertyChanged(); }
    }

    public string DetailText
    {
        get => _detailText;
        set { _detailText = value; OnPropertyChanged(); }
    }

    public string StatusColor
    {
        get => _statusColor;
        set { _statusColor = value; OnPropertyChanged(); }
    }

    public SolidColorBrush StatusBrush
    {
        get
        {
            var color = _instance.Status == InstanceStatus.Running ? "#00C853" : _instance.Status == InstanceStatus.Starting ? "#FFC107" : "#A0A0B0";
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }
    }

    public ICommand KillCommand => new RelayCommand(_ => Kill(), _ => _instance.Status == InstanceStatus.Running);

    public void UpdateStatus(InstanceStatus status)
    {
        _instance.Status = status;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        StatusText = _instance.Status == InstanceStatus.Running ? "Running" : _instance.Status == InstanceStatus.Starting ? "Starting..." : "Stopped";
        ButtonText = _instance.Status == InstanceStatus.Running ? "Kill" : "Launch";
        DetailText = _instance.Status == InstanceStatus.Running ? $"Running since {_instance.StartedAt:HH:mm:ss}" : _instance.LaunchArgs;
        StatusColor = _instance.Status == InstanceStatus.Running ? "#00C853" : _instance.Status == InstanceStatus.Starting ? "#FFC107" : "#A0A0B0";
    }

    private void Kill()
    {
        // Handled by parent
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
}
