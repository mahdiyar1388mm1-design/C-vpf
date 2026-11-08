using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusControlCenter.Data;
using NexusControlCenter.Services;
using NexusControlCenter.Repositories;
using NexusControlCenter.ViewModels;
using NexusControlCenter.Views;
using System.Windows;

namespace NexusControlCenter;

public partial class App : Application
{
    private IHost? _host;
    private async void OnStartup(object sender, StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) => { MessageBox.Show($"An unexpected error occurred.\n\n{args.Exception.Message}", "Nexus Control Center", MessageBoxButton.OK, MessageBoxImage.Error); args.Handled = true; };
        try
        {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddDbContextFactory<NexusDbContext>(options => options.UseSqlite($"Data Source={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NexusControlCenter", "nexus.db")}"));
                services.AddSingleton<SystemMetricsService>();
                services.AddSingleton<IActivityRepository, ActivityRepository>();
                services.AddSingleton<LoggingService>();
                services.AddSingleton<NotificationService>();
                services.AddSingleton<SettingsService>();
                services.AddSingleton<FileBrowserService>();
                services.AddSingleton<ExportService>();
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            }).Build();
        await _host.StartAsync();
        Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NexusControlCenter"));
        await using (var db = await _host.Services.GetRequiredService<IDbContextFactory<NexusDbContext>>().CreateDbContextAsync())
            await db.Database.EnsureCreatedAsync();
        var window = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
        await _host.Services.GetRequiredService<MainViewModel>().InitializeAsync();
        }
        catch (Exception ex) { MessageBox.Show($"Nexus could not start.\n\n{ex.Message}", "Nexus Control Center", MessageBoxButton.OK, MessageBoxImage.Error); Shutdown(-1); }
    }
    private async void OnExit(object sender, ExitEventArgs e)
    {
        if (_host is not null) { await _host.StopAsync(); _host.Dispose(); }
    }
}
