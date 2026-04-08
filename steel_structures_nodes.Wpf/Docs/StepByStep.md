# HingedJoint — Архитектура и алгоритм расчёта

## Содержание

- [Схема взаимодействия компонентов](#схема-взаимодействия-компонентов)
- [Каталог тестов](#каталог-тестов--111-тестов)
- [Поток данных](#схема-потока-данных)
- [Ссылки на файлы](#ссылки-на-файлы)
- [Переменные](#описание-расчёта-и-унификация-переменных)
- [Пошаговый алгоритм расчёта](#пошаговый-алгоритм-расчёта)
- [Маппинг переменных](#маппинг-переменных-код--json--ui)
- [Исправленные баги](#исправленные-баги)
- [Инструкция: добавление новой переменной](#добавление-новой-переменной-в-решение)
- [История изменений](#история-изменений)

---

## Схема взаимодействия компонентов

Решение состоит из **3 проектов**. Стрелка (→) означает зависимость/вызов.

```
HingedJoint.Wpf  (WPF App)
├── Views (XAML)
│   ├── MainWindow.xaml
│   └── Rs1MainView.xaml ◄── {Binding ...}
│
├── ViewModels
│   └── ViewModel
│       ├── _interactionService ──► InteractionTableService
│       ├── _excelReader ──► EpplusExcelReader
│       ├── StandardNode (StandardNodeInteractionViewModel)
│       ├── RsuRows / RsnRows / AnalysisRows / ComparisonItems / IdeaRows
│       ├── GammaF (γf) ◄── SolutionSettings
│       ├── ExecuteCalculation()
│       ├── LoadResultFromJson()
│       └── BuildComparisonChart()
│
├── Services
│   ├── InteractionTableService ──► interaction_tables.json (Assets/)
│   │   ├── LoadDistinctNames()
│   │   ├── LoadConnectionCodesByName()
│   │   ├── LoadStandardNode()
│   │   └── LoadProfileBeam/Column()
│   ├── SimpleJsonParser
│   ├── ClipboardRowParser ──► ForceRow
│   ├── EpplusExcelReader ──► ForceRow
│   ├── NodeImageService
│   ├── ConnectionOptionLoader
│   └── SolutionSettings (static) ──► solution_settings.json
│
├── Models
│   ├── StandardNodeData (Nt, Nc, N, Qy, Qz, My, Mz, ...)
│   ├── ConnectionOptionViewModel
│   └── ExcelImportRequest
│
└── ProjectReference ▼

HingedJoint.Calculate  (Class Library)
├── Calculates/
│   ├── Calculator : IRs1Calculator
│   │   ├── BuildAnalysisTable(rsu, rsn, filter)
│   │   │   ├── Шаг 1: pool = РСУ + РСН
│   │   │   ├── Шаг 2: фильтр элементов
│   │   │   ├── Шаг 3: FilterLastSection (min Sect)
│   │   │   ├── Шаг 4: 11 MAX-строк → AnalysisRow
│   │   │   └── Шаг 5: вычисление U, Psi
│   │   ├── ExtractSummary(analysisRows) → ForceRow.Summary*
│   │   └── CalculateAndSave(rsu, rsn, filter, dir) → Result_v001.json
│   ├── AnalysisRow (N, Nt, Nc, Qy, Qz, Mx, My, Mz, Mw, U, Psi, ...)
│   ├── RsuDataProcessor / RsnDataProcessor / DclSheetFormatter
│   └── ConnectionValidationManager
├── Models/
│   ├── ForceRow (DclNo, Elem, Sect, N, Mx, My, Qz, ...)
│   │   ├── Parsed*: ParsedN, ParsedMx, ... (string → double?)
│   │   ├── Album*: AlbumNc, AlbumQy, AlbumNt, AlbumPsi, ...
│   │   ├── Summary*: SummaryNt, SummaryNc, SummaryQy, MaxU, ...
│   │   └── AnalysisRows: List<AnalysisRow>
│   ├── RsuRow : ForceRow
│   └── RsnRow : ForceRow
└── Services/
    ├── IRs1Calculator / IExcelReader
    ├── IAlbumCapacityProvider
    │   ├── AlbumCapacityJsonProvider (album_capacities.json)
    │   └── AlbumCapacityProvider (заглушка)
    ├── Rs1ResultJsonSerializer (ToJson / FromJson)
    └── ExcelWorkbookInfo

         ▲ ProjectReference (Calculate + Wpf)

HingedJoint.Tests  (xUnit)
└── Тесты: Calculator, ForceRow, InteractionTableService, IDEA StatiCA и др.
```

---

## Каталог тестов — 111 тестов

```bash
dotnet test HingedJoint.Tests
```

CI/CD: `.github/workflows/ci.yml` (запуск на каждый push/PR)

### Группа 1: Расчёт (`Calculator.BuildAnalysisTable` + `ExtractSummary`)

#### `BuildAnalysisTableTests.cs` — общее поведение

| Тест | Что проверяет |
|------|---------------|
| `NullInputs_ReturnsEmpty` | null входы → пустая таблица (защита от NRE) |
| `EmptyInputs_ReturnsEmpty` | 0 строк РСУ + 0 строк РСН → пустая таблица |
| `Returns10Rows` | при наличии данных → ровно 11 MAX-строк |
| `CombinesRsuAndRsn` | РСУ и РСН объединяются в общий пул |
| `ElementFilter_Applied` | фильтр элементов ограничивает выборку |
| `FilterSection_KeepsMinSectionOnly` | фильтр по минимальному сечению (Sect=1) |
| `RowCopiesAllForcesFromSameCombination` | все усилия из одной комбинации |

#### `NtTests.cs` — MAX N+ (растяжение)

| Тест | Что проверяет |
|------|---------------|
| `SelectsRowWithMaxPositiveN` | выбирает строку с max(N>0) |
| `AllNegative_ReturnsEmptyRow` | если все N<0 → пустая строка |
| `SummaryNt_EqualsMaxPositiveN` | SummaryNt = Nt из MAX N+ |
| `SummaryNt_ZeroBecomes_Null` | Nt=0 → SummaryNt=null |

#### `NcTests.cs` — MAX N- (сжатие)

| Тест | Что проверяет |
|------|---------------|
| `SelectsRowWithMinNegativeN` | выбирает строку с min(N<0) |
| `AllPositive_ReturnsEmptyRow` | если все N>0 → пустая строка |
| `SummaryNc_EqualsMinNegativeN` | SummaryNc = Nc из MAX N- |
| `SummaryNc_ZeroBecomes_Null` | Nc=0 → null |

#### `QyTests.cs` — MAX Qy (поперечная сила)

| Тест | Что проверяет |
|------|---------------|
| `SelectsRowWithMaxAbsQy` | выбирает строку с max(\|Qy\|) |
| `SummaryQy_TakesAbsValue` | Summary берёт \|Qy\| (модуль) |
| `SummaryQy_ZeroBecomes_Null` | Qy=0 → null |

#### `QzTests.cs` — MAX Qz (поперечная сила, входит в формулу U)

| Тест | Что проверяет |
|------|---------------|
| `SelectsRowWithMaxAbsQz` | выбирает строку с max(\|Qz\|) |
| `SummaryQz_TakesAbsValue` | \|Qz\| в Summary |
| `SummaryQz_ZeroBecomes_Null` | Qz=0 → null |

#### `MxTests.cs` — MAX Mx (крутящий момент)

| Тест | Что проверяет |
|------|---------------|
| `SelectsRowWithMaxAbsMx` | выбирает строку с max(\|Mx\|) |
| `SummaryMx_TakesAbsValue` | \|Mx\| в Summary |
| `SummaryMx_ZeroBecomes_Null` | Mx=0 → null |

#### `MyTests.cs` — MAX My (изгибающий момент)

| Тест | Что проверяет |
|------|---------------|
| `SelectsRowWithMaxAbsMy` | выбирает строку с max(\|My\|) |
| `SummaryMy_TakesAbsValue` | \|My\| в Summary |
| `SummaryMy_ZeroBecomes_Null` | My=0 → null |

#### `MzTests.cs` — MAX Mz (изгибающий момент)

| Тест | Что проверяет |
|------|---------------|
| `SelectsRowWithMaxAbsMz` | выбирает строку с max(\|Mz\|) |
| `SummaryMz_TakesAbsValue` | \|Mz\| в Summary |
| `SummaryMz_ZeroBecomes_Null` | Mz=0 → null |

#### `MwTests.cs` — MAX Mw (бимомент)

| Тест | Что проверяет |
|------|---------------|
| `SelectsRowWithMaxAbsMw` | выбирает строку с max(\|Mw\|) |
| `SummaryMw_TakesAbsValue` | \|Mw\| в Summary |
| `SummaryMw_ZeroBecomes_Null` | Mw=0 → null |

#### `UTests.cs` — MAX Coeff / MAX u (коэффициент использования)

| Тест | Что проверяет |
|------|---------------|
| `MaxCoeff_Formula_QzDivAlbumQy_Plus_NDivAlbumNt` | формула U = \|Qz\|/AlbumQy + \|N\|/AlbumNt |
| `MaxU_SameAsMaxCoeff` | MAX u всегда совпадает с MAX Coeff |
| `NoAlbumCapacity_ReturnsEmptyRow` | AlbumQy=0 → пустая строка (без деления на 0) |

#### `PsiTests.cs` — коэффициент ψ

| Тест | Что проверяет |
|------|---------------|
| `AllRows_HavePsi_FromAlbum` | ψ из альбома проставляется всем 11 строкам |
| `SummaryPsi_FromAnalysisRows` | SummaryPsi = первое Psi из таблицы |

### Группа 2: Сохранение (`CalculateAndSave` + JSON round-trip)

#### `CalculateAndSaveTests.cs`

| Тест | Что проверяет |
|------|---------------|
| `WritesFile_And_IncrementsVersion` | создание `Result_v001.json`, `v002` (автоинкремент) |
| `JsonRoundtrip_PreservesData` | JSON → объект → проверка Version, 11 строк, SummaryNt/Nc |

### Группа 3: Маппинг IDEA StatiCA (`ViewModel.MapRowTypeToIdea`)

#### `IdeaMapRowTypeTests.cs`

| Тест | Что проверяет |
|------|---------------|
| `MapsKnownTypes` | MAX Qy→Qo, Qz→Q, Mx→T, My→M, Mz→Mo, N+→Nt, N-→Nc |
| `PreservesUnchangedTypes` | MAX Coeff, u, N, Mw — без изменений |
| `HandlesNullAndEmpty` | null/""/пробелы → возврат как есть |
| `AllAnalysisRows_HaveValidIdeaNames` | все 11 строк → валидные IDEA-имена |
| `IdeaRows_CountMatchesAnalysisRows` | 11 строк анализа → 11 строк IDEA |

#### `IdeaNTests.cs` — N в IDEA

| Тест | Что проверяет |
|------|---------------|
| `MaxN_RowType_PreservedAsMaxN` | MAX N → MAX N |
| `MaxNPlus_MappedTo_MaxNt` | MAX N+ → MAX Nt |

#### `IdeaVyTests.cs` — Vy (Qy → Vy)

| Тест | Что проверяет |
|------|---------------|
| `MaxQy_MappedTo_MaxQo` | MAX Qy → MAX Qo |
| `MaxQo_SelectsMaxAbsQy` | выбор строки с max \|Qy\| |

#### `IdeaVzTests.cs` — Vz (Qz → Vz)

| Тест | Что проверяет |
|------|---------------|
| `MaxQz_MappedTo_MaxQ` | MAX Qz → MAX Q |
| `MaxQ_SelectsMaxAbsQz` | выбор строки с max \|Qz\| |

#### `IdeaMxTests.cs` — Mx → T

| Тест | Что проверяет |
|------|---------------|
| `MaxMx_MappedTo_MaxT` | MAX Mx → MAX T |
| `MaxT_SelectsMaxAbsMx` | выбор строки с max \|Mx\| |

#### `IdeaMyTests.cs` — My → M

| Тест | Что проверяет |
|------|---------------|
| `MaxMy_MappedTo_MaxM` | MAX My → MAX M |
| `MaxM_SelectsMaxAbsMy` | выбор строки с max \|My\| |

#### `IdeaMzTests.cs` — Mz → Mo

| Тест | Что проверяет |
|------|---------------|
| `MaxMz_MappedTo_MaxMo` | MAX Mz → MAX Mo |
| `MaxMo_SelectsMaxAbsMz` | выбор строки с max \|Mz\| |

### Группа 4: Парсинг (ForceRow, SimpleJsonParser)

#### `ForceRowTests.cs`

| Тест | Что проверяет |
|------|---------------|
| `ParsedN_ParsesNumbers` | `"1.23"` → 1.23, `"1,23"` → 1.23, `"-2,5"` → -2.5 |
| `ParsedN_NullOrEmpty_ReturnsNull` | null/`""` → null |
| `ParsedFields_MultipleColumns` | парсинг N, Qy, Mx, My одновременно |

#### `SimpleJsonParserTests.cs`

| Тест | Что проверяет |
|------|---------------|
| `TryReadConnectionCodesFromArray_ReturnsCodes` | чтение CONNECTION_CODE из массива |
| `TryFindInteractionConnectionName_MatchesByNameAndBeam` | поиск по Name + ProfileBeam |

### Группа 5: Interaction (данные из `interaction_tables.json`)

> **Базовый класс:** `InteractionTestBase.cs` — загружает все ~25 000 записей за один проход O(N).
> Каждый тест проверяет, что `StandardNodeData` совпадает с «сырым» значением из JSON.

| Файл | Тест | JSON → свойство |
|------|------|-----------------|
| `InteractionProfileTests.cs` | `AllEntries_ProfileBeam_MatchRawJson` | `ProfileBeam` → `ProfileBeam` |
| | `AllEntries_ProfileColumn_MatchRawJson` | `ProfileColumn` → `ProfileColumn` |
| `InteractionSectionTests.cs` | `AllEntries_SectionH_MatchRawJson` | `H` → `SectionH` |
| | `AllEntries_SectionB_MatchRawJson` | `B` → `SectionB` |
| | `AllEntries_SectionS_MatchRawJson` | `s` → `SectionS` |
| | `AllEntries_SectionT_MatchRawJson` | `t` → `SectionT` |
| `InteractionNtTests.cs` | `AllEntries_Nt_MatchRawJson` | `Nt` → `Nt` |
| `InteractionNcTests.cs` | `AllEntries_Nc_MatchRawJson` | `Nc` → `Nc` |
| `InteractionNTests.cs` | `AllEntries_N_MatchRawJson` | `N` → `N` |
| `InteractionQyTests.cs` | `AllEntries_Qy_MatchRawJson` | `Qy` → `Qy` |
| `InteractionQzTests.cs` | `AllEntries_Qz_MatchRawJson` | `Qz` → `Qz` |
| `InteractionMxTests.cs` | `AllEntries_Mx_MatchRawJson` | `Mx` → `Mx` |
| `InteractionMyTests.cs` | `AllEntries_My_MatchRawJson` | `My` → `My` |
| `InteractionMzTests.cs` | `AllEntries_Mz_MatchRawJson` | `Mz` → `Mz` |
| `InteractionMwTests.cs` | `AllEntries_Mw_MatchRawJson` | `Mw` → `Mw` |
| `InteractionTTests.cs` | `AllEntries_T_MatchRawJson` | `T` → `T` |
| `InteractionMnegTests.cs` | `AllEntries_Mneg_MatchRawJson` | `Mneg` → `Mneg` |
| `InteractionSjTests.cs` | `AllEntries_Sj_MatchRawJson` | `Sj` → `Sj` |
| `InteractionSjoTests.cs` | `AllEntries_Sjo_MatchRawJson` | `Sjo` → `Sjo` |
| `InteractionVariableTests.cs` | `AllEntries_Variable_MatchRawJson` | `variable` → `Variable` |
| `InteractionGreekCoeffTests.cs` | `AllEntries_Alpha_MatchRawJson` | `α` → `Alpha` |
| | `AllEntries_Beta_MatchRawJson` | `β` → `Beta` |
| | `AllEntries_Gamma_MatchRawJson` | `γ` → `Gamma` |
| | `AllEntries_Delta_MatchRawJson` | `δ` → `Delta` |
| | `AllEntries_Epsilon_MatchRawJson` | `ε` → `Epsilon` |
| | `AllEntries_Lambda_MatchRawJson` | `λ` → `Lambda` |

---

## Схема потока данных

### Основной поток: расчёт (`ExecuteCalculation`)

```
Пользователь (Excel / буфер обмена)
       │ вставка / импорт
       ▼
ClipboardRowParser.ParseRows()  или  EpplusExcelReader.ReadRsuRsn()
       │ List<ForceRow>
       ▼
ViewModel.RsuRows / RsnRows  (ObservableCollection → UI)
       │
       │ ExecuteCalculation()
       │ γf = ParseCoeff(GammaF)
       │ if γf ≠ 1 → ScaleForceRows(rsu, γf)  (× коэффициент надёжности)
       ▼
Calculator.CalculateAndSave(rsu, rsn, elemFilter, resultDir)
  ├── IAlbumCapacityProvider.GetByKey("P1-P4-P6") ◄── album_capacities.json
  ├── BuildAnalysisTable → 11 × AnalysisRow
  ├── ExtractSummary → ForceRow (Summary*)
  ├── Rs1ResultJsonSerializer.ToJson()
  └── → Result_v001.json
       │
       │ LoadResultFromJson(path)
       ▼
ViewModel (UI заполняется из JSON)
  ├── AnalysisRows  (таблица в XAML)
  ├── Results       (сводка в XAML)
  └── ComparisonItems (диаграмма: табличные vs расчётные)
```

### Параллельный поток: загрузка табличных данных

```
interaction_tables.json (Assets/)
       │ InteractionTableService.LoadStandardNode(name, code)
       ▼
SimpleJsonParser → StandardNodeData
       │
       ▼
ViewModel.UpdateStandardNodeFromJson()
       │ StandardNode.Nt = f(data.Nt) ...
       ▼
StandardNodeInteractionViewModel (строки для UI)
       │ {Binding StandardNode.Nt}
       ▼
Rs1MainView.xaml (таблица несущей способности)
```

---

## Ссылки на файлы

### Проект: `HingedJoint.Calculate`

| Путь | Описание |
|------|----------|
| `Calculates\Calculator.cs` | Калькулятор РС1 (основная логика) |
| `Calculates\AnalysisRow.cs` | Модель строки анализа (MAX-строка) |
| `Calculates\RsuDataProcessor.cs` | Обработка данных РСУ |
| `Calculates\RsnDataProcessor.cs` | Обработка данных РСН |
| `Calculates\DclSheetFormatter.cs` | Форматирование листов DCL |
| `Calculates\ConnectionValidationManager.cs` | Валидация соединений |
| `Models\ForceRow.cs` | Базовая модель строки усилий |
| `Models\RSU\RsuRow.cs` | Строка РСУ (наследник ForceRow) |
| `Models\RSN\RsnRow.cs` | Строка РСН (наследник ForceRow) |
| `Services\IRs1Calculator.cs` | Интерфейс калькулятора |
| `Services\IExcelReader.cs` | Интерфейс чтения Excel |
| `Services\AlbumCapacityProvider.cs` | Интерфейс + заглушка альбома |
| `Services\AlbumCapacityJsonProvider.cs` | Провайдер альбома из JSON |
| `Services\Rs1ResultJsonSerializer.cs` | Сериализация результата в JSON |
| `Services\ExcelWorkbookInfo.cs` | Информация о книге Excel |

### Проект: `HingedJoint.Wpf`

| Путь | Описание |
|------|----------|
| `Views\MainWindow.xaml(.cs)` | Главное окно |
| `Views\Rs1MainView.xaml(.cs)` | Основной вид расчёта РС1 |
| `ViewModels\ViewModel.cs` | Главная ViewModel (+ `StandardNodeInteractionViewModel`) |
| `ViewModels\Rs1AnalysisRowViewModel.cs` | ViewModel строки анализа |
| `ViewModels\ResultItemViewModel.cs` | ViewModel строки сводки |
| `ViewModels\ComparisonChartItem.cs` | ViewModel элемента диаграммы |
| `ViewModels\IdeaStaticaRowViewModel.cs` | ViewModel строки IDEA StatiCA |
| `Models\StandardNodeData.cs` | Модель данных стандартного узла |
| `Models\ConnectionOptionViewModel.cs` | Модель опции соединения |
| `Models\ExcelImportRequest.cs` | Запрос импорта из Excel |
| `Services\InteractionTableService.cs` | Сервис таблиц взаимодействия (с кэшем) |
| `Services\SimpleJsonParser.cs` | Лёгкий парсер JSON |
| `Services\EpplusExcelReader.cs` | Чтение Excel через EPPlus |
| `Services\ClipboardRowParser.cs` | Парсинг строк из буфера обмена |
| `Services\NodeImageService.cs` | Загрузка изображений узлов |
| `Services\ConnectionOptionLoader.cs` | Загрузка опций соединений |
| `Services\ExcelImportDialogService.cs` | Диалог импорта из Excel |
| `Services\JsonAssetPathResolver.cs` | Резолвер пути к JSON-ассетам |
| `Services\RsuRsnHeaderMap.cs` | Маппинг заголовков РСУ/РСН |
| `Services\SolutionSettings.cs` | Глобальные настройки решения (γf) |
| `Mvvm\ViewModelBase.cs` | Базовый класс ViewModel (INPC) |
| `Mvvm\RelayCommand.cs` | Реализация ICommand |
| `Assets\interaction_tables.json` | Таблицы взаимодействия (статический ресурс) |
| `Assets\album_capacities.json` | Несущая способность узлов |

---

## Описание расчёта и унификация переменных

В решении используются **два набора переменных**:

**1) Расчётные** (вычисляются в `HingedJoint.Calculate`):

`N`, `Nt`, `Nc`, `Qy`, `Qz`, `Mx`, `My`, `Mz`, `Mw`, `T`, `Mneg`, `H`, `B`, `s`, `t`, `Sj`, `Sjo`

**2) Альбомные** (читаются из `interaction_tables.json`):

`AlbomN`, `AlbomNt`, `AlbomNc`, `AlbomQy`, `AlbomQz`, `AlbomMx`, `AlbomMy`, `AlbomMz`,
`AlbomMw`, `AlbomT`, `AlbomMneg`, `AlbomH`, `AlbomB`, `Albom_s`, `Albom_t`,
`AlbomSj`, `AlbomSjo`, `Albomα`, `Albomβ`, `Albomγ`, `Albomδ`, `Albomε`, `Albomλ`

---

## Пошаговый алгоритм расчёта

> Реализация: `Calculator.cs`

### Шаг 0. Входные данные

Из Excel/буфера обмена загружаются строки РСУ и РСН:

```
DclNo | [ElemType] | Elem | Sect | N | Mx | My | Qz | Mz | Qy | Mw
```

Свойства `Parsed*` (`ParsedN`, `ParsedMx`, ...) конвертируют строки в `double?`.

Также загружается альбом несущей способности (`cap`):
- `AlbumQy` — несущая способность по Qy (кН)
- `AlbumNt` — несущая способность по растяжению (кН)
- `AlbumPsi` — коэффициент ψ

> **Ссылки:** `ForceRow.cs` ~14-118, `EpplusExcelReader.ReadRsuRsn()`, `ClipboardRowParser.ParseRows()`, `AlbumCapacityJsonProvider.cs` ~17-58, `album_capacities.json`, `Calculator.cs` ~83: `var cap = _album?.GetByKey("P1-P4-P6")`

### Шаг 1. Объединение данных

`pool = РСУ + РСН`. Если оба пусты → пустая таблица.

> **Ссылки:** `Calculator.cs : BuildAnalysisTable()` ~99-102

### Шаг 2. Фильтр элементов

Если указан `ElementFilterText` → оставляем только указанные `Elem`. Если после фильтрации пусто → берём все.

> **Ссылки:** `Calculator.cs : BuildAnalysisTable()` ~104-110

### Шаг 3. Фильтр по сечению (`FilterLastSection`)

1. Найти `minSect = min(Sect)` среди всех строк
2. Оставить только строки с `Sect == minSect`
3. Если пусто → вернуть все строки

> **Ссылки:** `Calculator.cs : FilterLastSection()` ~321-347

### Шаг 4. Построение таблицы анализа (11 MAX-строк)

Из найденной строки копируются **все усилия целиком** (одна комбинация нагрузок):

```
N  = ParsedN ?? 0
Nt = N > 0 ? ParsedN : 0    (растяжение)
Nc = N < 0 ? ParsedN : 0    (сжатие)
Qy, Qz, Mx, My, Mz, Mw = Parsed* ?? 0
LoadCombination = DclNo
Element = Elem
```

> **Ссылки:** `Calculator.cs : RowToAnalysis()` ~250-268, `AnalysisRow.cs` ~8-24

---

#### \[0\] MAX N (максимум |N|)

Ищет строку с `max(|ParsedN|)`.

#### \[1\] MAX N+ (максимальное растяжение)

Метод: `BuildMaxNtRow`

1. `max = max(ParsedN)` только среди `ParsedN > 0`
2. Если нет положительных → пустая строка
3. Копируем все усилия из этой строки, `Nt = ParsedN`

**Пример:**

| Строка | N | Qz |
|--------|---|----|
| A | 50 | 10 |
| B | -30 | 20 |
| C | 15 | 5 |

→ max(N>0) = 50 → строка A → `Nt=50, N=50, Qz=10`

> **Ссылки:** `BuildMaxNtRow()` ~216-230, `MaxPositive()` ~369-380, `ExtractSummary()` ~415-421

---

#### \[2\] MAX N- (максимальное сжатие)

Метод: `BuildMaxNcRow`

1. `min = min(ParsedN)` только среди `ParsedN < 0`
2. Если нет отрицательных → пустая строка
3. Копируем все усилия, `Nc = ParsedN`

> **Ссылки:** `BuildMaxNcRow()` ~233-247, `MinNegative()` ~382-393

---

#### \[3\] MAX Qy

`max(|ParsedQy|)` → копия всех усилий из этой строки.

> **Ссылки:** `BuildMaxByColumn("MAX Qy", data, f => f.ParsedQy)` ~128, `QyTests.cs`

<details>
<summary>Детальные ссылки (MAX Qy)</summary>

- `Calculator.cs : BuildMaxByColumn()` ~197-213
- `Calculator.cs : MaxAbs()` ~356-367
- `ForceRow.cs : ParsedQy` ~62
- `AnalysisRow.cs : Qy` ~17
- `ExtractSummary()` ~424-425: `SummaryQy = |Qy|`
- `Rs1ResultJsonSerializer.cs : WriteSummary()` ~70: `SummaryQy`
- `ViewModel.cs : LoadResultFromJson()` ~616, ~635, ~645
- `ViewModel.cs : BuildComparisonChart()` ~964

</details>

---

#### \[4\] MAX Qz

`max(|ParsedQz|)`

> **Ссылки:** `BuildMaxByColumn("MAX Qz", data, f => f.ParsedQz)` ~131

---

#### \[5\] MAX Mx

`max(|ParsedMx|)`

> **Ссылки:** `BuildMaxByColumn("MAX Mx", data, f => f.ParsedMx)` ~134

---

#### \[6\] MAX My

`max(|ParsedMy|)`

> **Ссылки:** `BuildMaxByColumn("MAX My", data, f => f.ParsedMy)` ~137

---

#### \[7\] MAX Mz

`max(|ParsedMz|)`

> **Ссылки:** `BuildMaxByColumn("MAX Mz", data, f => f.ParsedMz)` ~140

---

#### \[8\] MAX Mw

`max(|ParsedMw|)`

> **Ссылки:** `BuildMaxByColumn("MAX Mw", data, f => f.ParsedMw)` ~143

---

#### \[9\] MAX Coeff (коэффициент использования)

Метод: `BuildMaxByU`

**Предусловие:** `AlbumQy ≠ 0` и `AlbumNt ≠ 0`. Иначе → пустая строка.

**Формула:**

```
U[i] = |ParsedQz[i]| / AlbumQy + |ParsedN[i]| / AlbumNt
```

**Пример** (AlbumQy=10, AlbumNt=100):

| Строка | Qz | N | U |
|--------|----|---|---|
| A | 3 | 50 | 0.3 + 0.5 = **0.8** |
| B | 7 | 10 | 0.7 + 0.1 = **0.8** |
| C | 9 | 5 | 0.9 + 0.05 = **0.95** ← max |

> **Ссылки:** `BuildMaxByU()` ~166-190, `UTests.cs` ~29-41

---

#### \[10\] MAX u

Дубль MAX Coeff (та же формула, всегда совпадает).

> **Ссылки:** `BuildMaxByU()` ~166-190, `UTests.cs : MaxU_SameAsMaxCoeff()` ~48-58

---

### Шаг 5. Вычисление U и Psi для всех строк

- `Psi = AlbumPsi` (одинаковый для всех)
- Для строк \[0\]–\[8\]: `U = |Qz| / AlbumQy + |N| / AlbumNt`

> **Ссылки:** `CalcU()` ~289-301, `BuildAnalysisTable()` ~152-157

### Шаг 6. Извлечение сводки (`ExtractSummary`)

| Свойство | Источник |
|----------|----------|
| `SummaryNt` | `Nt` из MAX N+ |
| `SummaryNc` | `Nc` из MAX N- |
| `SummaryQy` | \|Qy\| из MAX Qy |
| `SummaryQz` | \|Qz\| из MAX Qz |
| `SummaryMx` | \|Mx\| из MAX Mx |
| `SummaryMy` | \|My\| из MAX My |
| `SummaryMz` | \|Mz\| из MAX Mz |
| `SummaryMw` | \|Mw\| из MAX Mw |
| `MaxU` | max(U) по всем строкам |
| `SummaryPsi` | первое Psi из альбома |

> Если значение == 0 → `null`.
>
> **Ссылки:** `ExtractSummary()` ~396-467, `ForceRow.cs` — `SummaryNt` ~94, `SummaryNc` ~96, ... `MaxU` ~110

### Шаг 7. Сохранение (`CalculateAndSave`)

1. `nextVersion = max(существующие) + 1`
2. `BuildAnalysisTable` → `ExtractSummary` → `Rs1ResultJsonSerializer.ToJson`
3. Записать `Result_v{version:D3}.json`

<details>
<summary>Структура JSON</summary>

```json
{
  "version": 1,
  "summary": {
    "SummaryNt": ..., "SummaryNc": ..., "SummaryQy": ...,
    "SummaryQz": ..., "SummaryMx": ..., "SummaryMy": ...,
    "SummaryMz": ..., "SummaryMw": ..., "MaxU": ..., "Psi": ...
  },
  "analysisRows": [
    {
      "RowType": "MAX N+", "LoadCombination": "...", "Element": ...,
      "N": ..., "Nt": ..., "Nc": ..., "Qy": ..., "Qz": ...,
      "Mx": ..., "My": ..., "Mz": ..., "Mw": ..., "U": ..., "Psi": ...
    }
  ]
}
```

</details>

> **Ссылки:** `CalculateAndSave()` ~476-519, `Rs1ResultJsonSerializer.ToJson()` ~29-58, `ViewModel.cs : LoadResultFromJson()` ~593-651

---

## Маппинг переменных: код ↔ JSON ↔ UI

| Контекст | Переменные |
|----------|-----------|
| **ForceRow** (Excel вход) | `.N` `.Mx` `.My` `.Qz` `.Mz` `.Qy` `.Mw` (string) |
| **AnalysisRow** (таблица) | `.N` `.Nt` `.Nc` `.Qy` `.Qz` `.Mx` `.My` `.Mz` `.Mw` `.U` `.Psi` |
| **ForceRow** (Summary) | `.SummaryNt` `.SummaryNc` `.SummaryQy` `.SummaryQz` `.SummaryMx` `.SummaryMy` `.SummaryMz` `.MaxU` `.SummaryPsi` |
| **ForceRow** (Album) | `.AlbumNt` `.AlbumN` `.AlbumNc` `.AlbumQy` `.AlbumQz` `.AlbumMx` `.AlbumMy` `.AlbumMz` `.AlbumMw` `.AlbumT` `.AlbumPsi` |

**`interaction_tables.json`** → `StandardNodeData`:

| JSON-ключ | Свойство |
|-----------|----------|
| `Nt` | `Nt` |
| `Nc` | `Nc` |
| `N` | `N` |
| `Qy` | `Qy` |
| `Qz` | `Qz` |
| `T` | `T` |
| `My` | `My` |
| `Mz` | `Mz` |
| `Mneg` | `Mneg` |
| `H` / `B` / `s` / `t` | `SectionH` / `SectionB` / `SectionS` / `SectionT` |
| `Sj` / `Sjo` | `Sj` / `Sjo` |
| `α` `β` `γ` `δ` `ε` `λ` | `Alpha` `Beta` `Gamma` `Delta` `Epsilon` `Lambda` |

**`Result_vXXX.json`** (ключи сериализации):
- **summary:** `SummaryNt`, `SummaryNc`, `SummaryQy`, `SummaryQz`, `SummaryMz`, `SummaryMx`, `SummaryMy`, `MaxU`, `Psi`
- **rows:** `Qy`, `N`, `Qz`, `Mx`, `My`, `Mz`, `Mw`, `Nt`, `Nc`, `U`, `Psi`

---

## Исправленные баги

1. **InteractionTableService**: читал несуществующие JSON-ключи (`"AlbumNt"` вместо `"Nt"`).
   Результат: все альбомные значения были `null`.

2. **Rs1ResultJsonSerializer**: дублированные ключи `"Qz"` для Qy и Qz.
   Результат: `SummaryQz` всегда `null`.

3. **Calculator.ExtractSummary**: перепутаны Qy/Qz, мёртвые ветки `"MAX QX"`, `"MAX QO"`.
   Результат: `SummaryQy` содержал Qz.

4. **ForceRow**: `AlbumT`/`AlbumMy`/`AlbumPsi` использовались для расчётных сводок.
   Добавлены `SummaryMx`, `SummaryMy`, `SummaryPsi`.

---

## Добавление новой переменной в решение

Полный путь данных:

```
interaction_tables.json
  → SimpleJsonParser.TryGetNullableDouble()
  → InteractionTableService.LoadStandardNode()
  → StandardNodeData.NewVar
  → ViewModel.UpdateStandardNodeFromJson()
  → StandardNodeInteractionViewModel.NewVar (string)
  → Rs1MainView.xaml  {Binding StandardNode.NewVar}
```

| Шаг | Файл | Что делать |
|-----|------|------------|
| 1 | `Assets\interaction_tables.json` | Добавить `"NewVar": значение` |
| 2 | `Models\StandardNodeData.cs` | `public double? NewVar { get; set; }` |
| 3 | `Services\InteractionTableService.cs` | `NewVar = p.TryGetNullableDouble(row, "NewVar")` |
| 4 | `ViewModels\ViewModel.cs` (`StandardNodeInteractionViewModel`) | Поле, свойство, очистка |
| 5 | `ViewModels\ViewModel.cs` (`ViewModel`) | `StandardNode.NewVar = f(data.NewVar)` |
| 6 | `Views\Rs1MainView.xaml` | Столбец + `{Binding StandardNode.NewVar}` |
| 7* | `ViewModels\ViewModel.cs` | `BuildComparisonChart()` (опционально) |
| 8* | `ViewModels\ViewModel.cs` | `LoadResultFromJson()` (опционально) |

<details>
<summary>Подробности каждого шага</summary>

### Шаг 1. JSON

```json
{ "Name": "B", "CONNECTION_CODE": "B-1", "ProfileBeam": "L50×5", "NewVar": 42.5 }
```

### Шаг 2. Модель

```csharp
public double? NewVar { get; set; }
```

### Шаг 3. Сервис

```csharp
NewVar = p.TryGetNullableDouble(row, "NewVar"),
```

### Шаг 4. ViewModel отображения

```csharp
private string _newVar;
public string NewVar { get => _newVar; set { _newVar = value; OnPropertyChanged(); } }
// + в ClearAll(): NewVar = string.Empty;
```

### Шаг 5. Главная ViewModel

```csharp
StandardNode.NewVar = f(data.NewVar);
```

### Шаг 6. XAML

```xml
<Border Grid.Row="0" Grid.Column="23">
    <TextBlock Text="NewVar" FontWeight="SemiBold"/>
</Border>
<Border Grid.Row="1" Grid.Column="23">
    <TextBlock Text="{Binding StandardNode.NewVar}"/>
</Border>
```

</details>

---

## История изменений

### Коэффициент надёжности γf

- `ViewModel.cs` — свойство `GammaF`, `GammaFOptions` (1, 1.05, 1.1, 1.15, 1.2, 1.3)
- `SolutionSettings.cs` — загрузка/сохранение в `solution_settings.json`
- `Rs1MainView.xaml` — ComboBox с привязкой
- `ScaleForceRows()` / `ScaleValue()` — масштабирование РСУ/РСН перед расчётом
- `BuildComparisonChart()` — γf применяется к расчётным значениям

### Оптимизация InteractionTableService

- Добавлен `GetParser()` с кэшированием (JSON читается 1 раз)
- **Было:** каждый метод → `File.ReadAllText()` + `new SimpleJsonParser()`
- **Стало:** `GetParser()` возвращает кэш

### Оптимизация Interaction-тестов

- `SimpleJsonParser.TryReadAllInteractionRows()` — один проход по массиву
- `InteractionTestBase` — переписан конструктор
- **Было:** O(N²) — 25 000 × `LoadStandardNode()` → зависание
- **Стало:** O(N) — ~3 секунды

### Разделение Profile → ProfileBeam + ProfileColumn

- `StandardNodeData.cs` — `Profile` → `ProfileBeam` + `ProfileColumn`
- `SimpleJsonParser.cs` — все методы обновлены
- `InteractionTableService.cs` — раздельные методы загрузки
- `ViewModel.cs` — раздельные ComboBox и профили
- `Rs1MainView.xaml` — два ComboBox
