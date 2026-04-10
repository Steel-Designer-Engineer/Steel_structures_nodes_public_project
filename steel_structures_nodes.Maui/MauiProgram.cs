using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using steel_structures_nodes.Data.DependencyInjection;
using steel_structures_nodes.Domain.Contracts;
using steel_structures_nodes.Maui.Services;
using steel_structures_nodes.Maui.ViewModels;

namespace steel_structures_nodes.Maui;

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
        var dataAccessFailureNotifier = new DataAccessFailureNotifier();
        try
        {
            builder.Services.AddDataLayer(builder.Configuration, dataAccessFailureNotifier);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DataLayer registration failed: {ex.Message}");
            dataAccessFailureNotifier.Report("MauiProgram.AddDataLayer", "Не удалось инициализировать слой данных.", ex);
            throw;
        }

        // Redis для кэширования изображений
        var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
        builder.Services.AddSingleton(new RedisImageCacheService(redisConnectionString));

        // Сервис загрузки изображений: in-memory → Redis → MongoDB
        builder.Services.AddSingleton<INodeImageService>(sp =>
            new NodeImageService(
                sp.GetRequiredService<INodeImageRepository>(),
                sp.GetRequiredService<RedisImageCacheService>()));

        // Register ViewModels
        builder.Services.AddTransient<MainViewModel>();
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
