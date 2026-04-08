using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steel_structures_nodes_public_project.Data.DependencyInjection;
using Steel_structures_nodes_public_project.Wpf.Services;
using Steel_structures_nodes_public_project.Wpf.ViewModels;
using Steel_structures_nodes_public_project.Wpf.Views;

namespace Steel_structures_nodes_public_project.Wpf
{
    /// <summary>
    /// Главный класс WPF-приложения steel_structures_nodes.
    /// MongoDB настраивается автоматически через слой Data (steel_structures_nodes.Data).
    /// WPF работает только с репозиториями, не имея прямого доступа к MongoDB.
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }
        public IConfiguration Configuration { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += (s, args) =>
            {
                try
                {
                    MessageBox.Show(
                        $"Необработанная ошибка:\n{args.Exception.Message}\n\n{args.Exception.StackTrace}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch { }
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    MessageBox.Show(
                        $"Критическая ошибка:\n{ex.Message}\n\n{ex.StackTrace}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            try
            {
                Console.WriteLine("=== Application Startup ===");
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                Console.WriteLine($"Base Directory: {baseDir}");

                // Load configuration from appsettings.json
                var configPath = Path.Combine(baseDir, "appsettings.json");
                Console.WriteLine($"Looking for appsettings.json at: {configPath}");
                Console.WriteLine($"File exists: {File.Exists(configPath)}");

                var builder = new ConfigurationBuilder()
                    .SetBasePath(baseDir)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                Configuration = builder.Build();
                Console.WriteLine("✓ Configuration loaded successfully");

                // Log MongoDB configuration
                var mongoConnectionString = Configuration.GetConnectionString("MongoDB");
                var databaseName = Configuration.GetSection("DatabaseSettings:DatabaseName").Value;
                Console.WriteLine($"MongoDB will be configured by Data layer: {databaseName}");

                // Setup Dependency Injection
                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);
                ServiceProvider = serviceCollection.BuildServiceProvider();
                Console.WriteLine("✓ Dependency Injection configured");

                // Show main window
                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                Console.WriteLine("✓ MainWindow created");
                mainWindow.Show();
                Console.WriteLine("✓ MainWindow shown");
                Console.WriteLine("=== Application started successfully ===");
            }
            catch (FileNotFoundException ex)
            {
                var errorMsg = $"Файл конфигурации не найден:\n{ex.Message}\n\n" +
                    $"Ожидаемый путь: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json")}\n\n" +
                    $"Убедитесь, что файл steel_structures_nodes.Data\\appsettings.json существует и проект собран корректно.";
                Console.WriteLine($"✗ ERROR: {errorMsg}");
                MessageBox.Show(errorMsg, "Ошибка конфигурации", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
            catch (Exception ex)
            {
                var errorMsg = $"Ошибка при запуске приложения:\n{ex.Message}\n\n{ex.StackTrace}";
                Console.WriteLine($"✗ ERROR: {errorMsg}");
                MessageBox.Show(errorMsg, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Add Configuration
            services.AddSingleton(Configuration);

            // Add Logging
            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.SetMinimumLevel(LogLevel.Information);
            });

            // Register Data Layer with MongoDB
            // MongoDB connection is configured inside Data layer
            // WPF app only works with repositories
            Console.WriteLine("Registering Data Layer with MongoDB...");
            services.AddDataLayer(Configuration);
            Console.WriteLine("✓ Data Layer registered with repositories");

            // Register Application Services

            // Register Application Services
            services.AddTransient<WpfNodeImageService>();

            // Register ViewModels and Windows
            services.AddTransient<ViewModel>();
            services.AddTransient<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Console.WriteLine("=== Application Exit ===");
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
            base.OnExit(e);
        }
    }
}
