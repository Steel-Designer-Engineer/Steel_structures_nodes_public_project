using System.Collections.ObjectModel;
using System.Net.Sockets;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using steel_structures_nodes.Data.Contracts;
using steel_structures_nodes.Domain.Contracts;
using steel_structures_nodes.Domain.Entities;
using steel_structures_nodes.Maui.Controls;
using steel_structures_nodes.Maui.Services;

namespace steel_structures_nodes.Maui.ViewModels;

/// <summary>
/// Сводка фактических усилий из расчёта РС1 для отображения на графике.
/// </summary>
public record Rs1SummaryForces(
    double? N, double? Nt, double? Nc,
    double? Qy, double? Qz,
    double? Mx, double? My, double? Mz, double? Mw);

public partial class MainViewModel : ObservableObject
{
    private const string ExampleMongoHost = "example.mongodb.local";
    private const int ExampleMongoPort = 27017;

    private readonly IInteractionTableLookupRepository _interactionTableLookupRepository;
    private readonly IInteractionTableReadRepository _interactionTableReadRepository;
    private readonly IDataAccessFailureNotifier _dataAccessFailureNotifier;
    private readonly INodeImageService _nodeImageService;
    private bool _suppressCascade;

    public ExcelImportViewModel ExcelImport { get; }

    public MainViewModel(
        IInteractionTableLookupRepository interactionTableLookupRepository,
        IInteractionTableReadRepository interactionTableReadRepository,
        IDataAccessFailureNotifier dataAccessFailureNotifier,
        ExcelImportViewModel excelImport,
        INodeImageService nodeImageService)
    {
        _interactionTableLookupRepository = interactionTableLookupRepository;
        _interactionTableReadRepository = interactionTableReadRepository;
        _dataAccessFailureNotifier = dataAccessFailureNotifier ?? throw new ArgumentNullException(nameof(dataAccessFailureNotifier));
        _nodeImageService = nodeImageService;
        ExcelImport = excelImport;
        _dataAccessFailureNotifier.FailureOccurred += OnDataAccessFailure;
        // Подписка на результаты расчёта РС1
        excelImport.Rs1ResultsUpdated += actual => SetActualForces(actual);
        ConnectionNames = new ObservableCollection<string>();
        ProfileColumns  = new ObservableCollection<string>();
        ProfileBeams    = new ObservableCollection<string>();
        ConnectionCodes = new ObservableCollection<string>();

        if (_dataAccessFailureNotifier.LastFailure is not null)
            Status = _dataAccessFailureNotifier.LastFailure.Message;
    }

    private void OnDataAccessFailure(object? sender, DataAccessFailureEventArgs e)
    {
        Status = e.Message;
        System.Diagnostics.Debug.WriteLine($"Data access failure [{e.Source}]: {e.Exception}");
    }

    // --- Collections ---

    public ObservableCollection<string> ConnectionNames { get; }
    public ObservableCollection<string> ProfileColumns { get; }
    public ObservableCollection<string> ProfileBeams { get; }
    public ObservableCollection<string> ConnectionCodes { get; }

    // --- Selected values ---

    [ObservableProperty]
    private string? _selectedName;

    [ObservableProperty]
    private string? _selectedProfileColumn;

    [ObservableProperty]
    private string? _selectedProfileBeam;

    [ObservableProperty]
    private string? _selectedConnectionCode;

    // --- Node image ---

    private List<ImageSource> _nodeImages = [];
    private int _nodeImageIndex;

    [ObservableProperty] private ImageSource? _selectedNodeImage;
    [ObservableProperty] private string? _selectedNodeDescription;
    [ObservableProperty] private string? _imageIndexText;
    [ObservableProperty] private bool _hasMultipleImages;

    /// <summary>Пояснения из поля Explanations документа базы данных.</summary>
    [ObservableProperty] private string? _nodeExplanation;
    [ObservableProperty] private bool _hasExplanation;

    // --- Node data ---

    private InternalForcesData? _currentForces;
    private Rs1SummaryForces? _currentActual;

    /// <summary>
    /// Вызывается после перестроения графика для принудительной перерисовки GraphicsView.
    /// </summary>
    public event Action? ChartUpdated;

    [ObservableProperty] private string? _typeNode;
    [ObservableProperty] private string? _tableBrand;
    [ObservableProperty] private string? _status;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _hasData;

    // Несущая способность
    [ObservableProperty] private string? _ntValue;
    [ObservableProperty] private string? _ncValue;
    [ObservableProperty] private string? _nValue;
    [ObservableProperty] private string? _qyValue;
    [ObservableProperty] private string? _qzValue;
    [ObservableProperty] private string? _mxValue;
    [ObservableProperty] private string? _myValue;
    [ObservableProperty] private string? _mzValue;
    [ObservableProperty] private string? _mwValue;
    [ObservableProperty] private string? _mnegValue;

    // График несущей способности
    [ObservableProperty] private CapacityChartDrawable? _capacityChart;
    [ObservableProperty] private double _capacityChartHeight;

    // Жёсткость
    [ObservableProperty] private string? _sjValue;
    [ObservableProperty] private string? _sjoValue;

    // Пользовательские усилия для диаграммы
    [ObservableProperty] private string? _userNt;
    [ObservableProperty] private string? _userNc;
    [ObservableProperty] private string? _userN;
    [ObservableProperty] private string? _userQy;
    [ObservableProperty] private string? _userQz;
    [ObservableProperty] private string? _userMx;
    [ObservableProperty] private string? _userMy;
    [ObservableProperty] private string? _userMz;
    [ObservableProperty] private string? _userMw;

    // Коэффициенты
    [ObservableProperty] private string? _alphaValue;
    [ObservableProperty] private string? _betaValue;
    [ObservableProperty] private string? _gammaValue;
    [ObservableProperty] private string? _deltaValue;
    [ObservableProperty] private string? _epsilonValue;
    [ObservableProperty] private string? _lambdaValue;

    // Геометрия балки
    [ObservableProperty] private string? _beamProfile;
    [ObservableProperty] private string? _beamH;
    [ObservableProperty] private string? _beamB;
    [ObservableProperty] private string? _beamS;
    [ObservableProperty] private string? _beamT;
    [ObservableProperty] private string? _beamA;
    [ObservableProperty] private string? _beamP;
    [ObservableProperty] private string? _beamIz;
    [ObservableProperty] private string? _beamIy;
    [ObservableProperty] private string? _beamIx;
    [ObservableProperty] private string? _beamWz;
    [ObservableProperty] private string? _beamWy;
    [ObservableProperty] private string? _beamWx;
    [ObservableProperty] private string? _beamSz;
    [ObservableProperty] private string? _beamSy;
    [ObservableProperty] private string? _beamiz;
    [ObservableProperty] private string? _beamiy;
    [ObservableProperty] private string? _beamXo;

    // Геометрия колонны
    [ObservableProperty] private string? _columnProfile;
    [ObservableProperty] private string? _columnH;
    [ObservableProperty] private string? _columnB;
    [ObservableProperty] private string? _columnS;
    [ObservableProperty] private string? _columnT;
    [ObservableProperty] private string? _columnA;
    [ObservableProperty] private string? _columnP;
    [ObservableProperty] private string? _columnIz;
    [ObservableProperty] private string? _columnIy;
    [ObservableProperty] private string? _columnIx;
    [ObservableProperty] private string? _columnWz;
    [ObservableProperty] private string? _columnWy;
    [ObservableProperty] private string? _columnWx;
    [ObservableProperty] private string? _columnSz;
    [ObservableProperty] private string? _columnSy;
    [ObservableProperty] private string? _columniz;
    [ObservableProperty] private string? _columniy;
    [ObservableProperty] private string? _columnXo;
    [ObservableProperty] private string? _columnYo;

    // Пластина
    [ObservableProperty] private string? _plateH;
    [ObservableProperty] private string? _plateB;
    [ObservableProperty] private string? _plateT;

    // Фланец
    [ObservableProperty] private string? _flangeH;
    [ObservableProperty] private string? _flangeB;
    [ObservableProperty] private string? _flangeT;
    [ObservableProperty] private string? _flangeLb;

    // Рёбра жёсткости
    [ObservableProperty] private string? _stiffTr1;
    [ObservableProperty] private string? _stiffTr2;
    [ObservableProperty] private string? _stiffTbp;
    [ObservableProperty] private string? _stiffTg;
    [ObservableProperty] private string? _stiffTf;
    [ObservableProperty] private string? _stiffLh;
    [ObservableProperty] private string? _stiffHh;
    [ObservableProperty] private string? _stiffTwp;

    // Болты
    [ObservableProperty] private string? _boltCount;
    [ObservableProperty] private string? _boltRows;
    [ObservableProperty] private string? _boltDiameter;
    [ObservableProperty] private string? _boltVersion;
    [ObservableProperty] private string? _boltCoordZ;
    [ObservableProperty] private string? _boltE1;
    [ObservableProperty] private string? _boltP1;
    [ObservableProperty] private string? _boltP2;
    [ObservableProperty] private string? _boltP3;
    [ObservableProperty] private string? _boltP4;
    [ObservableProperty] private string? _boltP5;
    [ObservableProperty] private string? _boltP6;
    [ObservableProperty] private string? _boltP7;
    [ObservableProperty] private string? _boltP8;
    [ObservableProperty] private string? _boltP9;
    [ObservableProperty] private string? _boltP10;
    [ObservableProperty] private string? _boltD1;
    [ObservableProperty] private string? _boltD2;

    // Сварка
    [ObservableProperty] private string? _weldKf1;
    [ObservableProperty] private string? _weldKf2;
    [ObservableProperty] private string? _weldKf3;
    [ObservableProperty] private string? _weldKf4;
    [ObservableProperty] private string? _weldKf5;
    [ObservableProperty] private string? _weldKf6;
    [ObservableProperty] private string? _weldKf7;
    [ObservableProperty] private string? _weldKf8;
    [ObservableProperty] private string? _weldKf9;
    [ObservableProperty] private string? _weldKf10;

    // --- Init ---

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            IsBusy = true;
            var log = new System.Text.StringBuilder();

            // 1. TCP connectivity test
            log.AppendLine($"1) TCP → {ExampleMongoHost}:{ExampleMongoPort}...");
            Status = log.ToString();
            try
            {
                using var tcp = new TcpClient();
                var connectTask = tcp.ConnectAsync(ExampleMongoHost, ExampleMongoPort);
                if (await Task.WhenAny(connectTask, Task.Delay(5000)) == connectTask)
                {
                    await connectTask; // propagate exception if any
                    log.AppendLine("   ✅ TCP OK");
                }
                else
                {
                    log.AppendLine("   ❌ TCP таймаут (5с) — сервер недоступен из этой сети");
                    Status = log.ToString();
                    return;
                }
            }
            catch (Exception tcpEx)
            {
                log.AppendLine($"   ❌ TCP ошибка: {tcpEx.Message}");
                Status = log.ToString();
                return;
            }

            // 2. MongoDB ping
            log.AppendLine("2) MongoDB ping...");
            Status = log.ToString();
            try
            {
                var names = await _interactionTableLookupRepository.GetDistinctNamesAsync();
                log.AppendLine($"   ✅ MongoDB OK — {names.Count} групп");
            }
            catch (Exception mongoEx)
            {
                var inner = mongoEx.InnerException?.Message ?? "";
                log.AppendLine($"   ❌ MongoDB: {mongoEx.GetType().Name}: {mongoEx.Message}");
                if (!string.IsNullOrEmpty(inner))
                    log.AppendLine($"      Inner: {inner}");
            }

            Status = log.ToString();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadNamesAsync()
    {
        try
        {
            IsBusy = true;
            _suppressCascade = true;

            await LoadConnectionNamesAsync();
            if (!await TrySelectInitialConnectionNameAsync())
                return;

            Status = "Загрузка профилей…";
            await LoadProfileColumnsAsync(SelectedName!);
            await SelectFirstProfileColumnAsync();

            await LoadBeamsForColumnAsync();
            SelectFirstProfileBeam();

            Status = "Загрузка кодов…";
            await LoadConnectionCodesAsync();

            if (!string.IsNullOrWhiteSpace(SelectedName) && !string.IsNullOrWhiteSpace(SelectedConnectionCode))
                await LoadNodeDataAsync(SelectedName, SelectedConnectionCode);
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException?.Message ?? "";
            Status = $"Ошибка БД: [{ex.GetType().Name}] {ex.Message}" +
                     (string.IsNullOrEmpty(inner) ? "" : $" → {inner}");
            System.Diagnostics.Debug.WriteLine($"MongoDB error: {ex}");
        }
        finally
        {
            _suppressCascade = false;
            IsBusy = false;
        }
    }

    partial void OnSelectedNameChanged(string? value)
    {
        if (_suppressCascade || string.IsNullOrWhiteSpace(value)) return;
        UpdateSelectedNodePresentation(value);
        _ = SafeAsync(LoadProfilesAsync(value));
        _ = SafeAsync(LoadNodeImagesAsync(value));
    }

    partial void OnSelectedProfileColumnChanged(string? value)
    {
        if (_suppressCascade) return;
        _ = SafeAsync(OnColumnChangedAsync());
    }

    private async Task OnColumnChangedAsync()
    {
        try
        {
            _suppressCascade = true;

            await LoadBeamsForColumnAsync();

            await Task.Yield();
            if (ProfileBeams.Count > 0)
                SelectedProfileBeam = ProfileBeams[0];
            else
                SelectedProfileBeam = null;

            _suppressCascade = false;

            await LoadConnectionCodesAsync();
        }
        catch (Exception ex)
        {
            _suppressCascade = false;
            Status = $"Ошибка: {ex.Message}";
        }
    }

    partial void OnSelectedProfileBeamChanged(string? value)
    {
        if (_suppressCascade) return;
        _ = SafeAsync(LoadConnectionCodesAsync());
    }

    partial void OnSelectedConnectionCodeChanged(string? value)
    {
        if (_suppressCascade || string.IsNullOrWhiteSpace(SelectedName) || string.IsNullOrWhiteSpace(value)) return;
        _ = SafeAsync(LoadNodeDataAsync(SelectedName, value));
    }

    /// <summary>
    /// Обёртка для fire-and-forget — выводит ошибку в Status вместо молчаливого проглатывания.
    /// </summary>
    private async Task SafeAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            Status = $"Ошибка: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"SafeAsync error: {ex}");
        }
    }

    private async Task LoadProfilesAsync(string name)
    {
        try
        {
            _suppressCascade = true;

            await LoadProfileColumnsAsync(name);
            await SelectFirstProfileColumnAsync();

            await LoadBeamsForColumnAsync();
            SelectFirstProfileBeam();

            _suppressCascade = false;

            await LoadConnectionCodesAsync();
        }
        catch (Exception ex)
        {
            _suppressCascade = false;
            Status = $"Ошибка профилей: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"LoadProfilesAsync error: {ex}");
        }
    }

    /// <summary>
    /// Загружает список балок, доступных для текущей группы и выбранной колонны.
    /// </summary>
    private async Task LoadBeamsForColumnAsync()
    {
        ReplaceItems(ProfileBeams, await GetBeamsForCurrentSelectionAsync());
    }

    private async Task LoadConnectionCodesAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedName)) return;

        try
        {
            var codes = await GetConnectionCodesForCurrentSelectionAsync();
            ReplaceItems(ConnectionCodes, codes);

            if (ConnectionCodes.Count > 0)
            {
                // Даём Picker на Android обработать обновлённый ItemsSource
                await Task.Yield();
                SelectedConnectionCode = ConnectionCodes[0];
            }
        }
        catch (Exception ex)
        {
            Status = $"Ошибка кодов: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"LoadConnectionCodesAsync error: {ex}");
        }
    }

    private async Task LoadNodeDataAsync(string name, string connectionCode)
    {
        try
        {
            IsBusy = true;
            Status = "Загрузка узла...";

            var table = await _interactionTableReadRepository.GetByNameAndConnectionCodeAsync(name, connectionCode);
            ApplyLoadedNodeData(table, connectionCode);
            if (table != null)
                SynchronizeSelectionsWithLoadedNode(table);
        }
        catch (Exception ex)
        {
            Status = $"Ошибка: {ex.Message}";
            HasData = false;
            _suppressCascade = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyTableData(InteractionTable table)
    {
        ApplyHeaderData(table);
        ApplyExplanationData(table);
        ApplyForcesData(table.InternalForces);
        ApplyStiffnessData(table.Stiffness);
        ApplyCoefficientData(table.Coefficients);
        ApplyBeamGeometry(table.Geometry.Beam);
        ApplyColumnGeometry(table.Geometry.Column);
        ApplyPlateData(table.Geometry.Plate);
        ApplyFlangeData(table.Geometry.Flange);
        ApplyStiffenerData(table.Geometry.Stiff);
        ApplyBoltData(table.Bolts);
        ApplyWeldData(table.Welds);
    }

    private void UpdateSelectedNodePresentation(string? name)
    {
        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            SelectedNodeDescription = string.Empty;
            _nodeImages = [];
            _nodeImageIndex = 0;
            SelectedNodeImage = null;
            ImageIndexText = string.Empty;
            HasMultipleImages = false;
            return;
        }

        // Описание обновляем сразу; изображения загружаются асинхронно в LoadNodeImagesAsync
        SelectedNodeDescription = name;
        _nodeImages = [];
        _nodeImageIndex = 0;
        SelectedNodeImage = null;
        ImageIndexText = string.Empty;
        HasMultipleImages = false;
    }

    private async Task LoadNodeImagesAsync(string nodeCode)
    {
        try
        {
            var images = await _nodeImageService.LoadAllNodeImagesAsync(nodeCode);
            _nodeImages     = images;
            _nodeImageIndex = 0;
            SelectedNodeImage  = _nodeImages.Count > 0 ? _nodeImages[0] : null;
            HasMultipleImages  = _nodeImages.Count > 1;
            ImageIndexText     = _nodeImages.Count > 1 ? $"1 / {_nodeImages.Count}" : string.Empty;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadNodeImages error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void PrevImage()
    {
        if (_nodeImages.Count <= 1) return;
        _nodeImageIndex = (_nodeImageIndex - 1 + _nodeImages.Count) % _nodeImages.Count;
        SelectedNodeImage = _nodeImages[_nodeImageIndex];
        ImageIndexText = $"{_nodeImageIndex + 1} / {_nodeImages.Count}";
    }

    [RelayCommand]
    private void NextImage()
    {
        if (_nodeImages.Count <= 1) return;
        _nodeImageIndex = (_nodeImageIndex + 1) % _nodeImages.Count;
        SelectedNodeImage = _nodeImages[_nodeImageIndex];
        ImageIndexText = $"{_nodeImageIndex + 1} / {_nodeImages.Count}";
    }

    private void UpdateCapacityChart(InternalForcesData f, Rs1SummaryForces? actual = null, Rs1SummaryForces? user = null)
    {
        var forceColor = Color.FromArgb("#2B579A");   // Primary — силы
        var momentColor = Color.FromArgb("#217346");  // Accent — моменты
        var userColor = Color.FromArgb("#107C10");    // пользовательские усилия

        var items = new List<CapacityBarItem>
        {
            new("Nt, кН", f.Nt, forceColor, actual?.Nt, null, user?.Nt, userColor),
            new("Nc, кН", f.Nc, forceColor, actual?.Nc, null, user?.Nc, userColor),
            new("N, кН", f.N, forceColor, actual?.N, null, user?.N, userColor),
            new("Qy, кН", f.Qy, forceColor, actual?.Qy, null, user?.Qy, userColor),
            new("Qz, кН", f.Qz, forceColor, actual?.Qz, null, user?.Qz, userColor),
            new("Mx, кН·м", f.Mx, momentColor, actual?.Mx, null, user?.Mx, userColor),
            new("My, кН·м", f.My, momentColor, actual?.My, null, user?.My, userColor),
            new("Mz, кН·м", f.Mz, momentColor, actual?.Mz, null, user?.Mz, userColor),
            new("Mw, кН·м²", f.Mw, momentColor, actual?.Mw, null, user?.Mw, userColor),
        };

        var drawable = new CapacityChartDrawable();
        drawable.SetData(items);
        CapacityChart = drawable;
        CapacityChartHeight = drawable.RequiredHeight;
        ChartUpdated?.Invoke();
    }

    /// <summary>
    /// Обновляет график несущей способности, добавляя фактические усилия из расчёта РС1.
    /// Расчётные данные сохраняются и отображаются при переключении узлов.
    /// </summary>
    public void SetActualForces(Rs1SummaryForces actual)
    {
        _currentActual = actual;
        if (_currentForces == null) return;
        UpdateCapacityChart(_currentForces, actual, GetUserForces());
    }

    partial void OnUserNtChanged(string? value) => RebuildCapacityChartWithUserValues();
    partial void OnUserNcChanged(string? value) => RebuildCapacityChartWithUserValues();
    partial void OnUserNChanged(string? value) => RebuildCapacityChartWithUserValues();
    partial void OnUserQyChanged(string? value) => RebuildCapacityChartWithUserValues();
    partial void OnUserQzChanged(string? value) => RebuildCapacityChartWithUserValues();
    partial void OnUserMxChanged(string? value) => RebuildCapacityChartWithUserValues();
    partial void OnUserMyChanged(string? value) => RebuildCapacityChartWithUserValues();
    partial void OnUserMzChanged(string? value) => RebuildCapacityChartWithUserValues();
    partial void OnUserMwChanged(string? value) => RebuildCapacityChartWithUserValues();

    private void RebuildCapacityChartWithUserValues()
    {
        if (_currentForces == null) return;
        UpdateCapacityChart(_currentForces, _currentActual, GetUserForces());
    }

    private Rs1SummaryForces? GetUserForces()
    {
        static double? Parse(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var t = value.Trim().Replace(',', '.');
            return double.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d)
                ? d
                : null;
        }

        var nt = Parse(UserNt) ?? Parse(UserN);
        var nc = Parse(UserNc) ?? Parse(UserN);
        var n = Parse(UserN);
        var qy = Parse(UserQy);
        var qz = Parse(UserQz);
        var mx = Parse(UserMx);
        var my = Parse(UserMy);
        var mz = Parse(UserMz);
        var mw = Parse(UserMw);

        if (nt is null && nc is null && n is null && qy is null && qz is null && mx is null && my is null && mz is null && mw is null)
            return null;

        return new Rs1SummaryForces(n, nt, nc, qy, qz, mx, my, mz, mw);
    }

    [RelayCommand]
    private async Task CopyAllNodeDataAsync()
    {
        if (!HasData) return;

        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"Узел: {SelectedConnectionCode}");
        sb.AppendLine($"Группа: {SelectedName}");
        sb.AppendLine($"Марка: {TableBrand}");
        sb.AppendLine();

        // Несущая способность
        sb.AppendLine("═══ Несущая способность ═══");
        AppendLine(sb, "Nt, кН", NtValue);
        AppendLine(sb, "Nc, кН", NcValue);
        AppendLine(sb, "N, кН", NValue);
        AppendLine(sb, "Qy, кН", QyValue);
        AppendLine(sb, "Qz, кН", QzValue);
        AppendLine(sb, "Mx, кН·м", MxValue);
        AppendLine(sb, "My, кН·м", MyValue);
        AppendLine(sb, "Mz, кН·м", MzValue);
        AppendLine(sb, "Mw, кН·м²", MwValue);
        AppendLine(sb, "Mneg, кН·м", MnegValue);
        sb.AppendLine();

        // Жёсткость
        sb.AppendLine("═══ Жёсткость ═══");
        AppendLine(sb, "Sj, кН·м/рад", SjValue);
        AppendLine(sb, "Sjo, кН·м/рад", SjoValue);
        sb.AppendLine();

        // Коэффициенты
        sb.AppendLine("═══ Коэффициенты ═══");
        AppendLine(sb, "α", AlphaValue);
        AppendLine(sb, "β", BetaValue);
        AppendLine(sb, "γ", GammaValue);
        AppendLine(sb, "δ", DeltaValue);
        AppendLine(sb, "ε", EpsilonValue);
        AppendLine(sb, "λ", LambdaValue);
        sb.AppendLine();

        // Геометрия колонны
        sb.AppendLine($"═══ Колонна: {ColumnProfile} ═══");
        AppendLine(sb, "H, мм", ColumnH);
        AppendLine(sb, "B, мм", ColumnB);
        AppendLine(sb, "s, мм", ColumnS);
        AppendLine(sb, "t, мм", ColumnT);
        AppendLine(sb, "A, см²", ColumnA);
        AppendLine(sb, "P, кг/м", ColumnP);
        AppendLine(sb, "Ix, см⁴", ColumnIx);
        AppendLine(sb, "Iy, см⁴", ColumnIy);
        AppendLine(sb, "Iz, см⁴", ColumnIz);
        AppendLine(sb, "Wy, см³", ColumnWy);
        AppendLine(sb, "Wz, см³", ColumnWz);
        AppendLine(sb, "Wx, см³", ColumnWx);
        AppendLine(sb, "Sy, см³", ColumnSy);
        AppendLine(sb, "Sz, см³", ColumnSz);
        AppendLine(sb, "iy, см", Columniy);
        AppendLine(sb, "iz, см", Columniz);
        AppendLine(sb, "xo, см", ColumnXo);
        AppendLine(sb, "yo, см", ColumnYo);
        sb.AppendLine();

        // Геометрия балки
        sb.AppendLine($"═══ Балка: {BeamProfile} ═══");
        AppendLine(sb, "H, мм", BeamH);
        AppendLine(sb, "B, мм", BeamB);
        AppendLine(sb, "s, мм", BeamS);
        AppendLine(sb, "t, мм", BeamT);
        AppendLine(sb, "A, см²", BeamA);
        AppendLine(sb, "P, кг/м", BeamP);
        AppendLine(sb, "Ix, см⁴", BeamIx);
        AppendLine(sb, "Iy, см⁴", BeamIy);
        AppendLine(sb, "Iz, см⁴", BeamIz);
        AppendLine(sb, "Wy, см³", BeamWy);
        AppendLine(sb, "Wz, см³", BeamWz);
        AppendLine(sb, "Wx, см³", BeamWx);
        AppendLine(sb, "Sy, см³", BeamSy);
        AppendLine(sb, "Sz, см³", BeamSz);
        AppendLine(sb, "iy, см", Beamiy);
        AppendLine(sb, "iz, см", Beamiz);
        AppendLine(sb, "xo, см", BeamXo);
        sb.AppendLine();

        // Пластина
        sb.AppendLine("═══ Пластина ═══");
        AppendLine(sb, "H, мм", PlateH);
        AppendLine(sb, "B, мм", PlateB);
        AppendLine(sb, "t, мм", PlateT);
        sb.AppendLine();

        // Фланец
        sb.AppendLine("═══ Фланец ═══");
        AppendLine(sb, "H, мм", FlangeH);
        AppendLine(sb, "B, мм", FlangeB);
        AppendLine(sb, "t, мм", FlangeT);
        AppendLine(sb, "Lb, мм", FlangeLb);
        sb.AppendLine();

        // Рёбра жёсткости
        sb.AppendLine("═══ Рёбра жёсткости ═══");
        AppendLine(sb, "tr1, мм", StiffTr1);
        AppendLine(sb, "tr2, мм", StiffTr2);
        AppendLine(sb, "tbp, мм", StiffTbp);
        AppendLine(sb, "tg, мм", StiffTg);
        AppendLine(sb, "tf, мм", StiffTf);
        AppendLine(sb, "twp, мм", StiffTwp);
        AppendLine(sb, "Lh, мм", StiffLh);
        AppendLine(sb, "Hh, мм", StiffHh);
        sb.AppendLine();

        // Болты
        sb.AppendLine("═══ Болты ═══");
        AppendLine(sb, "Кол-во", BoltCount);
        AppendLine(sb, "Рядов", BoltRows);
        AppendLine(sb, "Диаметр, мм", BoltDiameter);
        AppendLine(sb, "Версия", BoltVersion);
        AppendLine(sb, "Коорд. Z", BoltCoordZ);
        AppendLine(sb, "e1", BoltE1);
        AppendLine(sb, "p1", BoltP1);
        AppendLine(sb, "p2", BoltP2);
        AppendLine(sb, "p3", BoltP3);
        AppendLine(sb, "p4", BoltP4);
        AppendLine(sb, "p5", BoltP5);
        AppendLine(sb, "p6", BoltP6);
        AppendLine(sb, "p7", BoltP7);
        AppendLine(sb, "p8", BoltP8);
        AppendLine(sb, "p9", BoltP9);
        AppendLine(sb, "p10", BoltP10);
        AppendLine(sb, "d1", BoltD1);
        AppendLine(sb, "d2", BoltD2);
        sb.AppendLine();

        // Сварка
        sb.AppendLine("═══ Сварка ═══");
        AppendLine(sb, "kf1", WeldKf1);
        AppendLine(sb, "kf2", WeldKf2);
        AppendLine(sb, "kf3", WeldKf3);
        AppendLine(sb, "kf4", WeldKf4);
        AppendLine(sb, "kf5", WeldKf5);
        AppendLine(sb, "kf6", WeldKf6);
        AppendLine(sb, "kf7", WeldKf7);
        AppendLine(sb, "kf8", WeldKf8);
        AppendLine(sb, "kf9", WeldKf9);
        AppendLine(sb, "kf10", WeldKf10);

        // Примечания
        if (HasExplanation)
        {
            sb.AppendLine();
            sb.AppendLine("═══ Примечания ═══");
            sb.AppendLine(NodeExplanation);
        }

        await Clipboard.Default.SetTextAsync(sb.ToString());
        Status = "✓ Данные узла скопированы в буфер обмена";
    }

    private static void AppendLine(System.Text.StringBuilder sb, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && value != "—")
            sb.AppendLine($"  {label}: {value}");
    }

    private static string Format(double value) => value == 0 ? "—" : value.ToString("G6");

    private static string Format(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        var t = value.Trim();
        var normalized = t.Replace(',', '.');
        if (double.TryParse(normalized, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d))
            return d == 0 ? "—" : d.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
        return t;
    }
}
