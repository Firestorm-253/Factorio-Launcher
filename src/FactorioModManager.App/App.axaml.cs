using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FactorioModManager.App.Factorio;
using FactorioModManager.App.Services;
using FactorioModManager.App.ViewModels;
using FactorioModManager.App.Views;

namespace FactorioModManager.App;

public sealed partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            var dialogService = new DialogService(window);
            var appSettingsService = new AppSettingsService();
            var modInfoReader = new ModInfoReader();
            var modListReader = new ModListReader();
            var modListMetadataService = new ModListMetadataService();
            var viewModel = new MainWindowViewModel(
                dialogService,
                appSettingsService,
                new FolderValidator(),
                new FactorioInstallLocator(),
                new FactorioGameLauncher(),
                new FactorioGameRunningDetector(),
                new ModScanner(modInfoReader),
                new ModListDetector(modListReader, modListMetadataService),
                new ModListWriter(),
                modListMetadataService,
                new ModSettingsManager(),
                new BackupService(),
                new ModListActivator(new BackupService()),
                new ModListFileManager(),
                new NameValidator(),
                new ActiveModListDetector());

            window.DataContext = viewModel;
            window.Opened += async (_, _) => await viewModel.InitializeAsync();
            var gameStateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            gameStateTimer.Tick += (_, _) => viewModel.RefreshFactorioRunningState();
            window.Opened += (_, _) => gameStateTimer.Start();
            window.Closed += (_, _) => gameStateTimer.Stop();
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
