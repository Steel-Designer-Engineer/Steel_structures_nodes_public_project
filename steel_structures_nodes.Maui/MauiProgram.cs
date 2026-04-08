using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steel_structures_nodes_public_project.Data.DependencyInjection;
using Steel_structures_nodes_public_project.Domain.Repositories;
using Steel_structures_nodes_public_project.Maui.Services;
using Steel_structures_nodes_public_project.Maui.ViewModels;

namespace Steel_structures_nodes_public_project.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Load configuration from embedded appsettings.json
        // Task.Run снимает операцию с UI SynchronizationContext — устраняет дедлок на Android
        using var stream = Task.Run(() => FileSystem.OpenAppPackageFileAsync("appsettings.json")).GetAwaiter().GetResult();
        var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        builder.Configuration.AddConfiguration(config);

        // Register Data layer (MongoDB repositories)
        try
        {
            builder.Services.AddDataLayer(builder.Configuration);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DataLayer registration failed: {ex.Message}");
            // Fallback: заглушки, чтобы приложение запустилось в offline-режиме
            builder.Services.AddSingleton<IInteractionTableRepository>(
                new OfflineInteractionTableRepository(ex.Message));
            builder.Services.AddSingleton<INodeImageRepository>(
                new OfflineNodeImageRepository());
        }

        // Сервис загрузки изображений из MongoDB
        builder.Services.AddTransient<NodeImageService>(sp =>
            new NodeImageService(sp.GetRequiredService<INodeImageRepository>()));

        // Register ViewModels
        builder.Services.AddTransient<MainViewModel>(sp =>
        {
            var repo        = sp.GetRequiredService<IInteractionTableRepository>();
            var excelImport = sp.GetRequiredService<ExcelImportViewModel>();
            var imgService  = sp.GetRequiredService<NodeImageService>();
            return new MainViewModel(repo, excelImport, imgService);
        });
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<ExcelImportViewModel>(sp =>
        {
            var repo = sp.GetService<ICalculationResultRepository>();
            var logger = sp.GetService<ILogger<ExcelImportViewModel>>();
            return (repo is not null && logger is not null)
                ? new ExcelImportViewModel(repo, logger)
                : new ExcelImportViewModel();
        });
        builder.Services.AddTransient<ExcelImportPage>();

        return builder.Build();
    }
}
