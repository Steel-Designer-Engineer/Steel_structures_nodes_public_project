# Настройка HingedJoint.Data для MongoDB

## Обзор

Проект HingedJoint.Data настроен для работы с MongoDB в качестве хранилища данных. Все данные, которые раньше хранились в JSON-файлах в папке `HingedJoint.Wpf\Assets\`, теперь хранятся в MongoDB.

## Структура данных

### Коллекции MongoDB

База данных располагается по адресу: **example.mongodb.local:27017**
База данных: **HingedJointDB**


1. **AlbumCapacities** - несущая способность узлов из альбома (мигрируется из JSON)
2. **all_node** - таблицы взаимодействия узлов (используется InteractionTableRepository)
3. **profile** - профили металлоконструкций (используется ProfileRepository)
4. **PageContent** - контент страниц (если используется)
5. **Users** - пользователи системы
6. **SessionHistory** - история сессий пользователей

**Важно:** 
- Репозиторий `IInteractionTableRepository` читает данные из существующей коллекции `all_node`
- Репозиторий `IProfileRepository` читает данные из существующей коллекции `profile`
- Только `AlbumCapacities` требует миграции из JSON файлов

## Удаленные зависимости

Из проекта HingedJoint.Data удалены следующие компоненты, которые не используются для данного решения:

### Удаленные NuGet пакеты:
- **`AspNetCore.Identity.Mongo` (версия 9.0.0)** 
  - Пакет для интеграции ASP.NET Core Identity с MongoDB
  - Не используется, так как проект не требует сложной системы аутентификации и авторизации
  - Простая доменная модель `User` достаточна для текущих требований

- **`StackExchange.Redis` (версия 2.8.16)**
  - Клиент для работы с Redis
  - Не используется, так как кэширование и распределенные кэши не требуются
  - MongoDB достаточно для всех потребностей хранения данных

### Удаленные файлы:
- **`HingedJoint.Data\Identity\ApplicationUser.cs`**
  - Класс, наследующий `MongoUser` из AspNetCore.Identity.Mongo
  - Удален вместе с зависимостью от AspNetCore.Identity.Mongo
  - Заменен простой доменной сущностью `User` в `HingedJoint.Domain\Entities\User.cs`

### Оставшиеся пакеты:
- `MongoDB.Driver` (версия 2.28.0) - основной драйвер для работы с MongoDB
- `Microsoft.Extensions.DependencyInjection.Abstractions` (версия 8.0.2) - для dependency injection
- `Microsoft.Extensions.Logging.Abstractions` (версия 8.0.2) - для логирования
- `Microsoft.Extensions.Configuration.Abstractions` (версия 8.0.0) - для конфигурации

## Настройка подключения к MongoDB

### 1. Доступ к MongoDB

MongoDB сервер располагается по адресу: **example.mongodb.local:27017**
База данных: **HingedJointDB**
Аутентификация: задаётся локально через `appsettings.json`, переменные окружения или user-secrets

### 2. Настройте строку подключения

В вашем приложении (например, в `appsettings.json`):


### 3. Регистрация сервисов

В вашем Startup или Program.cs:

```csharp
using HingedJoint.Data.DependencyInjection;
using MongoDB.Driver;

// Настройка MongoDB
var mongoConnectionString = configuration.GetConnectionString("MongoDB");
var databaseName = configuration.GetConnectionString("DatabaseName");
var mongoClient = new MongoClient(mongoConnectionString);
var database = mongoClient.GetDatabase(databaseName);
services.AddSingleton<IMongoDatabase>(database);

// Регистрация репозиториев и сервисов Data Layer
services.AddDataLayer();
```

Для локальной разработки можно переопределить значения через переменные окружения:

```powershell
$env:STEEL_ConnectionStrings__MongoDB = "mongodb://username:password@example.mongodb.local:27017/?authSource=admin"
$env:STEEL_DatabaseSettings__DatabaseName = "HingedJointDB"
```

Или через `user-secrets`:

```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:MongoDB" "mongodb://username:password@example.mongodb.local:27017/?authSource=admin"
dotnet user-secrets set "DatabaseSettings:DatabaseName" "HingedJointDB"
```

## Миграция данных из JSON в MongoDB

**Важно:** Данные для `InteractionTable` и `Profile` уже находятся в MongoDB (коллекции `all_node` и `profile`). Миграция требуется только для `AlbumCapacities`.

### Однократная миграция

Для первичной загрузки данных из JSON-файлов в MongoDB используйте `DataMigrationUtility`:

```csharp
using HingedJoint.Data.Migration;

var mongoConnectionString = "mongodb://username:password@example.mongodb.local:27017/?authSource=admin";
var databaseName = "HingedJointDB";

var albumCapacitiesPath = @"C:\path\to\project\HingedJoint.Wpf\Assets\album_capacities.json";
// interaction_tables.json НЕ ИСПОЛЬЗУЕТСЯ - данные в коллекции 'all_node'
// Profile.json НЕ ИСПОЛЬЗУЕТСЯ - данные в коллекции 'profile'

await DataMigrationUtility.MigrateFromJsonFilesAsync(
    mongoConnectionString,
    databaseName,
    albumCapacitiesPath,
    null, // interaction_tables.json не используется
    null  // Profile.json не используется
);
```

### Создание тестового проекта для миграции

Можно создать простое консольное приложение:

```csharp
using HingedJoint.Data.Migration;

await DataMigrationUtility.MigrateFromJsonFilesAsync(
    args[0], // MongoDB connection string
    args[1], // Database name
    args[2], // album_capacities.json path
    args[3], // interaction_tables.json path
    args[4]  // Profile.json path
);

Console.WriteLine("Migration completed!");
```

## Использование репозиториев

### Пример использования IAlbumCapacityRepository

```csharp
public class MyService
{
    private readonly IAlbumCapacityRepository _albumCapacityRepository;

    public MyService(IAlbumCapacityRepository albumCapacityRepository)
    {
        _albumCapacityRepository = albumCapacityRepository;
    }

    public async Task<AlbumCapacity> GetCapacityAsync(string key)
    {
        return await _albumCapacityRepository.GetByKeyAsync(key);
    }
}
```

### Пример использования IInteractionTableRepository

```csharp
public class InteractionService
{
    private readonly IInteractionTableRepository _repository;

    public InteractionService(IInteractionTableRepository repository)
    {
        _repository = repository;
    }

    public async Task<string[]> GetNamesAsync()
    {
        var names = await _repository.GetDistinctNamesAsync();
        return names.ToArray();
    }

    public async Task<InteractionTable> GetTableAsync(string name, string connectionCode)
    {
        return await _repository.GetByNameAndConnectionCodeAsync(name, connectionCode);
    }
}
```

### Пример использования IProfileRepository

```csharp
public class ProfileService
{
    private readonly IProfileRepository _repository;

    public ProfileService(IProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Profile> GetProfileAsync(string profileName)
    {
        // Данные читаются из коллекции 'profile' в MongoDB
        return await _repository.GetByNameAsync(profileName);
    }

    public async Task<IEnumerable<Profile>> GetAllProfilesAsync()
    {
        return await _repository.GetAllAsync();
    }
}
```

## Тестирование

После миграции данных можно проверить их наличие в MongoDB:

1. Откройте MongoDB Compass или mongosh
2. Подключитесь к mongodb
3. Выберите базу данных `HingedJointDB`
4. Проверьте коллекции: 
   - `AlbumCapacities` - данные из `album_capacities.json` (если была миграция)
   - `all_node` - таблицы взаимодействия (должны уже существовать)
   - `profile` - профили металлоконструкций (должны уже существовать)

## Замечания

- MongoDB сервер располагается по адресу **example.mongodb.local:27017**, база данных **HingedJointDB**
- **Коллекция `all_node` должна уже существовать** с данными таблиц взаимодействия
- **Коллекция `profile` должна уже существовать** с данными профилей металлоконструкций
- JSON-файлы `interaction_tables.json` и `Profile.json` **НЕ ИСПОЛЬЗУЮТСЯ** - данные читаются напрямую из MongoDB
- JSON-файл `album_capacities.json` используется только для первичной миграции (если требуется)
- Все данные управляются через репозитории в слое Data
- Индексы создаются автоматически при первом обращении к коллекциям

## Следующие шаги

1. Убедитесь, что в MongoDB (`example.mongodb.local:27017`) существует база данных `HingedJointDB` с коллекциями:
   - `all_node` (таблицы взаимодействия)
   - `profile` (профили металлоконструкций)
2. При необходимости мигрируйте данные для `AlbumCapacities` из JSON
3. Настройте строку подключения к MongoDB в вашем приложении
4. Обновите код в проектах HingedJoint.Calculate и HingedJoint.Wpf для использования репозиториев
5. JSON-файлы `interaction_tables.json` и `Profile.json` можно архивировать или удалить (они больше не используются)
