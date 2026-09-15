using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
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
    private string _activeCountText = "0/3";
    private string _versionText = "Version 1.2.1";
    private string _statusText = "Ready";
    private string _updateStatusText = "";
    private string _emptyMessage = "No instances running. Click Launch Instance to get started.";

    public ObservableCollection<RobloxInstanceViewModel> Instances { get; set; } = new();

    public string DisplayName { get => _displayName; set { _displayName = value; OnPropertyChanged(); OnPropertyChanged(nameof(AvatarInitial)); } }
    public string AvatarInitial => string.IsNullOrEmpty(DisplayName) ? "?" : DisplayName.Trim().Substring(0, 1).ToUpperInvariant();
    public string ActiveCountText { get => _activeCountText; set { _activeCountText = value; OnPropertyChanged(); } }
    public string VersionText { get => _versionText; set { _versionText = value; OnPropertyChanged(); } }
    public string StatusText { get => _statusText; set { SetThreadSafe(() => _statusText = value, nameof(StatusText)); } }
    public string UpdateStatusText { get => _updateStatusText; set { SetThreadSafe(() => _updateStatusText = value, nameof(UpdateStatusText)); } }
    public string EmptyMessage { get => _emptyMessage; set { SetThreadSafe(() => _emptyMessage = value, nameof(EmptyMessage)); } }

    public ICommand LaunchCommand { get; }
    public ICommand KillAllCommand { get; }
    public ICommand CheckUpdateCommand { get; }

    public MainWindowViewModel()
    {
        _instanceService = new InstanceService();
        _updateService = new UpdateService(Constants.AppVersion);
        _robloxService = new RobloxService();

        LaunchCommand = new RelayCommand(async _ => await LaunchInstanceAsync(), _ => _instanceService.CanLaunchInstance());
        KillAllCommand = new RelayCommand(_ => KillAllInstances(), _ => Instances.Count > 0);
        CheckUpdateCommand = new RelayCommand(async _ => await CheckForUpdates());

        LoadUserProfile();
        UpdateEmptyMessage();
        StartUpdateChecker();
    }

    private void LoadUserProfile()
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

    private async Task LaunchInstanceAsync()
    {
        StatusText = "Launching instance...";
        var instance = await _instanceService.LaunchInstanceAsync();
        if (instance != null)
        {
            RobloxInstanceViewModel vm = null!;
            vm = new RobloxInstanceViewModel(instance, _ => KillInstance(vm));
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

    private void KillInstance(RobloxInstanceViewModel vm)
    {
        _instanceService.KillInstance(vm.RobloxInstance.Id);
        vm.UpdateStatus(InstanceStatus.Stopped);
        Instances.Remove(vm);
        UpdateCounts();
        UpdateEmptyMessage();
        StatusText = $"Instance {vm.RobloxInstance.Id} killed.";
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
                var result = MessageBox.Show($"Update v{update.Version} is available!\n\nDownload and install now?", "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                {
                    var newExe = await _updateService.DownloadLatestReleaseAsync();
                    if (newExe != null)
                    {
                        UpdateStatusText = "Installing update...";
                        if (_updateService.ApplyUpdate(newExe))
                        {
                            UpdateStatusText = "Update applied. Restarting...";
                            // Shut down; the helper script replaces the exe and restarts.
                            Application.Current.Shutdown();
                        }
                        else
                        {
                            UpdateStatusText = "Update installation failed.";
                        }
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
        timer.Elapsed += async (s, e) =>
        {
            if (Application.Current?.Dispatcher.CheckAccess() == true)
            {
                await CheckForUpdates();
            }
            else
            {
                await Application.Current.Dispatcher.InvokeAsync(async () => await CheckForUpdates());
            }
        };
        timer.AutoReset = true;
        timer.Enabled = true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void SetThreadSafe(Action setter, string propertyName)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
        {
            setter();
            OnPropertyChanged(propertyName);
            return;
        }
        if (dispatcher.CheckAccess())
        {
            setter();
            OnPropertyChanged(propertyName);
        }
        else
        {
            dispatcher.Invoke(() =>
            {
                setter();
                OnPropertyChanged(propertyName);
            });
        }
    }
}

public class RobloxInstanceViewModel : INotifyPropertyChanged
{
    private readonly RobloxInstance _instance;
    private readonly Action<RobloxInstanceViewModel> _onKill;
    private string _statusText = "Stopped";
    private string _buttonText = "Launch";
    private string _detailText = "";

    public RobloxInstanceViewModel(RobloxInstance instance, Action<RobloxInstanceViewModel> onKill)
    {
        _instance = instance;
        _onKill = onKill;
        _detailText = instance.LaunchArgs;
    }

    public string Name => _instance.Name;
    public RobloxInstance RobloxInstance => _instance;
    public InstanceStatus Status => _instance.Status;

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
        StatusText = status == InstanceStatus.Running ? "Running" : status == InstanceStatus.Starting ? "Starting..." : "Stopped";
        ButtonText = status == InstanceStatus.Running ? "Kill" : "Launch";
        DetailText = status == InstanceStatus.Running ? $"Running since {_instance.StartedAt:HH:mm:ss}" : _instance.LaunchArgs;
        OnPropertyChanged(nameof(StatusBrush));
    }

    private void Kill()
    {
        _onKill?.Invoke(this);
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
