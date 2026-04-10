using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using steel_structures_nodes.Data.Contracts;
using steel_structures_nodes.Domain.Contracts;
using steel_structures_nodes.Domain.Fallback;
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
            .AddEnvironmentVariables(prefix: "STEEL_")
            .Build();

        builder.Configuration.AddConfiguration(config);

        var dataAccessFailureNotifier = new DataAccessFailureNotifier();
        var unavailableMessage = "Слой данных недоступен: проект steel_structures_nodes.Data отсутствует в решении.";
        dataAccessFailureNotifier.Report("MauiProgram", unavailableMessage);

        var interactionRepository = new UnavailableInteractionTableRepository(unavailableMessage);

        builder.Services.AddSingleton<IDataAccessFailureNotifier>(dataAccessFailureNotifier);
        builder.Services.AddSingleton<IInteractionTableRepository>(interactionRepository);
        builder.Services.AddSingleton<IInteractionTableLookupRepository>(interactionRepository);
        builder.Services.AddSingleton<IInteractionTableReadRepository>(interactionRepository);
        builder.Services.AddSingleton<ICalculationResultRepository, UnavailableCalculationResultRepository>();
        builder.Services.AddSingleton<INodeImageRepository, UnavailableNodeImageRepository>();

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
