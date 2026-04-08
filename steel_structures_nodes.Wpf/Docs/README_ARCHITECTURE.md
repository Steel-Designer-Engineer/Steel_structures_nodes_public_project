# Архитектура: Разделение ответственности WPF и Data Layer

## Проблема (было)

WPF приложение напрямую работало с MongoDB:
```csharp
// ❌ НЕПРАВИЛЬНО - WPF создавал MongoClient
var mongoClient = new MongoClient(mongoConnectionString);
var database = mongoClient.GetDatabase(databaseName);
services.AddSingleton<IMongoClient>(mongoClient);
services.AddSingleton<IMongoDatabase>(database);
services.AddDataLayer(); // без параметров
```

## Решение (стало)

Вся логика MongoDB инкапсулирована в Data Layer:

### WPF Application (App.xaml.cs)
```csharp
// ✅ ПРАВИЛЬНО - WPF только вызывает AddDataLayer с конфигурацией
private void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton(Configuration);
    services.AddLogging(...);
    
    // Data Layer сам настраивает MongoDB внутри
    services.AddDataLayer(Configuration);
    
    services.AddScoped<DataService>();
    services.AddTransient<MainWindow>();
}
```

### Data Layer (DataLayerExtensions.cs)
```csharp
// ✅ MongoDB настраивается внутри Data Layer
public static IServiceCollection AddDataLayer(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    // Читаем конфигурацию
    var mongoConnectionString = configuration.GetConnectionString("MongoDB");
    var databaseName = configuration["DatabaseSettings:DatabaseName"];
    
    // Настраиваем MongoDB (WPF не знает об этом)
    var mongoClient = new MongoClient(mongoConnectionString);
    var database = mongoClient.GetDatabase(databaseName);
    
    services.AddSingleton<IMongoClient>(mongoClient);
    services.AddSingleton<IMongoDatabase>(database);
    
    // Регистрируем репозитории
    services.AddScoped<IProfileRepository>(...);
    services.AddScoped<IInteractionTableRepository>(...);
    // ...
    
    return services;
}
```

## Преимущества новой архитектуры

### 1. Разделение ответственности (SRP)
- **WPF**: UI логика, ViewModels, DataService
- **Data Layer**: Подключение к БД, репозитории, миграции

### 2. Инкапсуляция
- WPF не знает о MongoDB
- WPF не знает о строке подключения
- WPF не создает MongoClient

### 3. Единая точка входа
```csharp
// Один метод для регистрации всего Data Layer
services.AddDataLayer(Configuration);
```

### 4. Тестируемость
- Можно подменить реализацию Data Layer
- Можно протестировать WPF без MongoDB
- Можно протестировать Data Layer независимо

### 5. Переиспользование
- Data Layer можно использовать в других приложениях
- Web API, Console App, другое WPF приложение
- Все используют `services.AddDataLayer(config)`

## Структура проектов

```
HingedJoint.Wpf (Presentation Layer)
├── Views
├── ViewModels
├── Services
│   └── DataService.cs         // Обертка над репозиториями
└── App.xaml.cs                // services.AddDataLayer(config)

HingedJoint.Data (Data Access Layer)
├── MongoDB
│   ├── ProfileRepository.cs
│   ├── InteractionTableRepository.cs
│   └── ...
├── DataLayerExtensions
│   └── DataLayerExtensions.cs // MongoDB + репозитории
├── Migration
└── appsettings.json           // MongoDB конфигурация

HingedJoint.Domain (Domain Layer)
├── Entities
│   ├── Profile.cs
│   ├── InteractionTable.cs
│   └── ...
└── Repositories (Interfaces)
    ├── IProfileRepository.cs
    └── IInteractionTableRepository.cs
```

## Поток данных

```
1. WPF загружает appsettings.json
2. WPF вызывает AddDataLayer(config)
3. Data Layer читает config
4. Data Layer создает MongoClient
5. Data Layer регистрирует репозитории
6. WPF получает репозитории через DI
7. WPF использует DataService
8. DataService использует репозитории
9. Репозитории работают с MongoDB
```

## Важно

- ❌ WPF **НЕ ДОЛЖЕН** создавать MongoClient
- ❌ WPF **НЕ ДОЛЖЕН** знать о MongoDB
- ✅ WPF **ДОЛЖЕН** использовать только `AddDataLayer(config)`
- ✅ Data Layer **ДОЛЖЕН** инкапсулировать всю логику БД
- ✅ Один источник истины: `HingedJoint.Data\appsettings.json`
