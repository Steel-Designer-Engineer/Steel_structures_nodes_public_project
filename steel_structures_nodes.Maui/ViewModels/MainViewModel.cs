using System.Collections.ObjectModel;
using System.Net.Sockets;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Steel_structures_nodes_public_project.Domain.Entities;
using Steel_structures_nodes_public_project.Domain.Repositories;
using Steel_structures_nodes_public_project.Maui.Controls;
using Steel_structures_nodes_public_project.Maui.Services;

namespace Steel_structures_nodes_public_project.Maui.ViewModels;

/// <summary>
/// Сводка фактических усилий из расчёта РС1 для отображения на графике.
/// </summary>
public record Rs1SummaryForces(
    double? N, double? Nt, double? Nc,
    double? Qy, double? Qz,
    double? Mx, double? My, double? Mz, double? Mw);

public partial class MainViewModel : ObservableObject
{
    private readonly IInteractionTableRepository _repository;
    private readonly NodeImageService _nodeImageService;
    private bool _suppressCascade;

    public ExcelImportViewModel ExcelImport { get; }

    public MainViewModel(
        IInteractionTableRepository repository,
        ExcelImportViewModel excelImport,
        NodeImageService nodeImageService)
    {
        _repository       = repository;
        _nodeImageService = nodeImageService;
        ExcelImport       = excelImport;
        // Подписка на результаты расчёта РС1
        excelImport.Rs1ResultsUpdated += actual => SetActualForces(actual);
        ConnectionNames = new ObservableCollection<string>();
        ProfileColumns  = new ObservableCollection<string>();
        ProfileBeams    = new ObservableCollection<string>();
        ConnectionCodes = new ObservableCollection<string>();
    }

    // --- Collections ---

    public ObservableCollection<string> ConnectionNames { get; }
    public ObservableCollection<string> ProfileColumns { get; }
    public ObservableCollection<string> ProfileBeams { get; }
    public ObservableCollection<string> ConnectionCodes { get; }

    // --- Selected values ---

    [ObservableProperty]
    public partial string? SelectedName { get; set; }

    [ObservableProperty]
    public partial string? SelectedProfileColumn { get; set; }

    [ObservableProperty]
    public partial string? SelectedProfileBeam { get; set; }

    [ObservableProperty]
    public partial string? SelectedConnectionCode { get; set; }

    // --- Node image ---

    private List<ImageSource> _nodeImages = [];
    private int _nodeImageIndex;

    [ObservableProperty] public partial ImageSource? SelectedNodeImage { get; set; }
    [ObservableProperty] public partial string? SelectedNodeDescription { get; set; }
    [ObservableProperty] public partial string? ImageIndexText { get; set; }
    [ObservableProperty] public partial bool HasMultipleImages { get; set; }

    /// <summary>Пояснения из поля Explanations документа базы данных.</summary>
    [ObservableProperty] public partial string? NodeExplanation { get; set; }
    [ObservableProperty] public partial bool HasExplanation { get; set; }

    // --- Node data ---

    private InternalForcesData? _currentForces;
    private Rs1SummaryForces? _currentActual;

    /// <summary>
    /// Вызывается после перестроения графика для принудительной перерисовки GraphicsView.
    /// </summary>
    public event Action? ChartUpdated;

    [ObservableProperty] public partial string? TableBrand { get; set; }
    [ObservableProperty] public partial string? Status { get; set; }
    [ObservableProperty] public partial bool IsBusy { get; set; }
    [ObservableProperty] public partial bool HasData { get; set; }

    // Несущая способность
    [ObservableProperty] public partial string? NtValue { get; set; }
    [ObservableProperty] public partial string? NcValue { get; set; }
    [ObservableProperty] public partial string? NValue { get; set; }
    [ObservableProperty] public partial string? QyValue { get; set; }
    [ObservableProperty] public partial string? QzValue { get; set; }
    [ObservableProperty] public partial string? MxValue { get; set; }
    [ObservableProperty] public partial string? MyValue { get; set; }
    [ObservableProperty] public partial string? MzValue { get; set; }
    [ObservableProperty] public partial string? MwValue { get; set; }
    [ObservableProperty] public partial string? MnegValue { get; set; }

    // График несущей способности
    [ObservableProperty] public partial CapacityChartDrawable? CapacityChart { get; set; }
    [ObservableProperty] public partial double CapacityChartHeight { get; set; }

    // Жёсткость
    [ObservableProperty] public partial string? SjValue { get; set; }
    [ObservableProperty] public partial string? SjoValue { get; set; }

    // Коэффициенты
    [ObservableProperty] public partial string? AlphaValue { get; set; }
    [ObservableProperty] public partial string? BetaValue { get; set; }
    [ObservableProperty] public partial string? GammaValue { get; set; }
    [ObservableProperty] public partial string? DeltaValue { get; set; }
    [ObservableProperty] public partial string? EpsilonValue { get; set; }
    [ObservableProperty] public partial string? LambdaValue { get; set; }

    // Геометрия балки
    [ObservableProperty] public partial string? BeamProfile { get; set; }
    [ObservableProperty] public partial string? BeamH { get; set; }
    [ObservableProperty] public partial string? BeamB { get; set; }
    [ObservableProperty] public partial string? BeamS { get; set; }
    [ObservableProperty] public partial string? BeamT { get; set; }
    [ObservableProperty] public partial string? BeamA { get; set; }
    [ObservableProperty] public partial string? BeamP { get; set; }
    [ObservableProperty] public partial string? BeamIz { get; set; }
    [ObservableProperty] public partial string? BeamIy { get; set; }
    [ObservableProperty] public partial string? BeamIx { get; set; }
    [ObservableProperty] public partial string? BeamWz { get; set; }
    [ObservableProperty] public partial string? BeamWy { get; set; }
    [ObservableProperty] public partial string? BeamWx { get; set; }
    [ObservableProperty] public partial string? BeamSz { get; set; }
    [ObservableProperty] public partial string? BeamSy { get; set; }
    [ObservableProperty] public partial string? Beamiz { get; set; }
    [ObservableProperty] public partial string? Beamiy { get; set; }
    [ObservableProperty] public partial string? BeamXo { get; set; }

    // Геометрия колонны
    [ObservableProperty] public partial string? ColumnProfile { get; set; }
    [ObservableProperty] public partial string? ColumnH { get; set; }
    [ObservableProperty] public partial string? ColumnB { get; set; }
    [ObservableProperty] public partial string? ColumnS { get; set; }
    [ObservableProperty] public partial string? ColumnT { get; set; }
    [ObservableProperty] public partial string? ColumnA { get; set; }
    [ObservableProperty] public partial string? ColumnP { get; set; }
    [ObservableProperty] public partial string? ColumnIz { get; set; }
    [ObservableProperty] public partial string? ColumnIy { get; set; }
    [ObservableProperty] public partial string? ColumnIx { get; set; }
    [ObservableProperty] public partial string? ColumnWz { get; set; }
    [ObservableProperty] public partial string? ColumnWy { get; set; }
    [ObservableProperty] public partial string? ColumnWx { get; set; }
    [ObservableProperty] public partial string? ColumnSz { get; set; }
    [ObservableProperty] public partial string? ColumnSy { get; set; }
    [ObservableProperty] public partial string? Columniz { get; set; }
    [ObservableProperty] public partial string? Columniy { get; set; }
    [ObservableProperty] public partial string? ColumnXo { get; set; }
    [ObservableProperty] public partial string? ColumnYo { get; set; }

    // Пластина
    [ObservableProperty] public partial string? PlateH { get; set; }
    [ObservableProperty] public partial string? PlateB { get; set; }
    [ObservableProperty] public partial string? PlateT { get; set; }

    // Фланец
    [ObservableProperty] public partial string? FlangeH { get; set; }
    [ObservableProperty] public partial string? FlangeB { get; set; }
    [ObservableProperty] public partial string? FlangeT { get; set; }
    [ObservableProperty] public partial string? FlangeLb { get; set; }

    // Рёбра жёсткости
    [ObservableProperty] public partial string? StiffTr1 { get; set; }
    [ObservableProperty] public partial string? StiffTr2 { get; set; }
    [ObservableProperty] public partial string? StiffTbp { get; set; }
    [ObservableProperty] public partial string? StiffTg { get; set; }
    [ObservableProperty] public partial string? StiffTf { get; set; }
    [ObservableProperty] public partial string? StiffLh { get; set; }
    [ObservableProperty] public partial string? StiffHh { get; set; }
    [ObservableProperty] public partial string? StiffTwp { get; set; }

    // Болты
    [ObservableProperty] public partial string? BoltCount { get; set; }
    [ObservableProperty] public partial string? BoltRows { get; set; }
    [ObservableProperty] public partial string? BoltDiameter { get; set; }
    [ObservableProperty] public partial string? BoltVersion { get; set; }
    [ObservableProperty] public partial string? BoltCoordZ { get; set; }
    [ObservableProperty] public partial string? BoltE1 { get; set; }
    [ObservableProperty] public partial string? BoltP1 { get; set; }
    [ObservableProperty] public partial string? BoltP2 { get; set; }
    [ObservableProperty] public partial string? BoltP3 { get; set; }
    [ObservableProperty] public partial string? BoltP4 { get; set; }
    [ObservableProperty] public partial string? BoltP5 { get; set; }
    [ObservableProperty] public partial string? BoltP6 { get; set; }
    [ObservableProperty] public partial string? BoltP7 { get; set; }
    [ObservableProperty] public partial string? BoltP8 { get; set; }
    [ObservableProperty] public partial string? BoltP9 { get; set; }
    [ObservableProperty] public partial string? BoltP10 { get; set; }
    [ObservableProperty] public partial string? BoltD1 { get; set; }
    [ObservableProperty] public partial string? BoltD2 { get; set; }

    // Сварка
    [ObservableProperty] public partial string? WeldKf1 { get; set; }
    [ObservableProperty] public partial string? WeldKf2 { get; set; }
    [ObservableProperty] public partial string? WeldKf3 { get; set; }
    [ObservableProperty] public partial string? WeldKf4 { get; set; }
    [ObservableProperty] public partial string? WeldKf5 { get; set; }
    [ObservableProperty] public partial string? WeldKf6 { get; set; }
    [ObservableProperty] public partial string? WeldKf7 { get; set; }
    [ObservableProperty] public partial string? WeldKf8 { get; set; }
    [ObservableProperty] public partial string? WeldKf9 { get; set; }
    [ObservableProperty] public partial string? WeldKf10 { get; set; }

    // --- Init ---

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            IsBusy = true;
            var log = new System.Text.StringBuilder();

            // 1. TCP connectivity test
            log.AppendLine("1) TCP → 72.56.72.77:32017...");
            Status = log.ToString();
            try
            {
                using var tcp = new TcpClient();
                var connectTask = tcp.ConnectAsync("72.56.72.77", 32017);
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
                var names = await _repository.GetDistinctNamesAsync();
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

            if (_repository is Services.OfflineInteractionTableRepository offline)
            {
                Status = $"БД недоступна: {offline.ErrorMessage}";
                return;
            }

            // 1. Группы
            Status = "Загрузка групп…";
            var names = await _repository.GetDistinctNamesAsync();
            ConnectionNames.Clear();
            foreach (var n in names)
                ConnectionNames.Add(n);

            if (ConnectionNames.Count == 0)
            {
                Status = "Нет данных в БД";
                return;
            }

            await Task.Yield();
            SelectedName = ConnectionNames[0];
            UpdateSelectedNodePresentation(SelectedName);
            _ = SafeAsync(LoadNodeImagesAsync(SelectedName!));

            // 2. Профили
            Status = "Загрузка профилей…";
            var cols = await _repository.GetDistinctProfileColumnsByNameAsync(SelectedName);
            ProfileColumns.Clear();
            foreach (var c in cols)
                ProfileColumns.Add(c);

            var beams = await _repository.GetDistinctProfileBeamsByNameAsync(SelectedName);
            ProfileBeams.Clear();
            foreach (var b in beams)
                ProfileBeams.Add(b);

            await Task.Yield();
            if (ProfileColumns.Count > 0)
                SelectedProfileColumn = ProfileColumns[0];
            if (ProfileBeams.Count > 0)
                SelectedProfileBeam = ProfileBeams[0];

            // 3. Коды соединений
            Status = "Загрузка кодов…";
            await LoadConnectionCodesAsync();

            // 4. Данные узла
            if (!string.IsNullOrWhiteSpace(SelectedName) && !string.IsNullOrWhiteSpace(SelectedConnectionCode))
            {
                Status = "Загрузка узла…";
                var table = await _repository.GetByNameAndConnectionCodeAsync(SelectedName, SelectedConnectionCode);
                if (table != null)
                {
                    ApplyTableData(table);
                    HasData = true;
                    Status = $"Узел: {SelectedConnectionCode}";
                }
                else
                {
                    HasData = false;
                    Status = "Узел не найден";
                }
            }
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
        _ = SafeAsync(LoadConnectionCodesAsync());
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

            var columns = await _repository.GetDistinctProfileColumnsByNameAsync(name);
            ProfileColumns.Clear();
            foreach (var c in columns)
                ProfileColumns.Add(c);

            var beams = await _repository.GetDistinctProfileBeamsByNameAsync(name);
            ProfileBeams.Clear();
            foreach (var b in beams)
                ProfileBeams.Add(b);

            // Даём Picker на Android обработать обновлённые коллекции
            await Task.Yield();

            if (ProfileColumns.Count > 0)
                SelectedProfileColumn = ProfileColumns[0];
            if (ProfileBeams.Count > 0)
                SelectedProfileBeam = ProfileBeams[0];

            _suppressCascade = false;

            // Один явный вызов вместо двух параллельных через property-changed
            await LoadConnectionCodesAsync();
        }
        catch (Exception ex)
        {
            _suppressCascade = false;
            Status = $"Ошибка профилей: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"LoadProfilesAsync error: {ex}");
        }
    }

    private async Task LoadConnectionCodesAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedName)) return;

        try
        {
            IReadOnlyList<string> codes;

            if (!string.IsNullOrWhiteSpace(SelectedProfileColumn) && !string.IsNullOrWhiteSpace(SelectedProfileBeam))
                codes = await _repository.GetConnectionCodesByNameColumnAndBeamAsync(SelectedName, SelectedProfileColumn, SelectedProfileBeam);
            else if (!string.IsNullOrWhiteSpace(SelectedProfileColumn))
                codes = await _repository.GetConnectionCodesByNameAndColumnAsync(SelectedName, SelectedProfileColumn);
            else if (!string.IsNullOrWhiteSpace(SelectedProfileBeam))
                codes = await _repository.GetConnectionCodesByNameAndBeamAsync(SelectedName, SelectedProfileBeam);
            else
                codes = await _repository.GetConnectionCodesByNameAsync(SelectedName);

            ConnectionCodes.Clear();
            foreach (var c in codes)
                ConnectionCodes.Add(c);

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

            var table = await _repository.GetByNameAndConnectionCodeAsync(name, connectionCode);
            if (table == null)
            {
                HasData = false;
                Status = "Узел не найден";
                return;
            }

            ApplyTableData(table);
            HasData = true;
            Status = $"Узел: {connectionCode}";
        }
        catch (Exception ex)
        {
            Status = $"Ошибка: {ex.Message}";
            HasData = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyTableData(InteractionTable table)
    {
        TableBrand = table.TableBrand;

        // Пояснения из поля Explanations документа
        NodeExplanation  = string.IsNullOrWhiteSpace(table.Explanations) ? null : table.Explanations;
        HasExplanation   = !string.IsNullOrWhiteSpace(table.Explanations);

        // Несущая способность
        var f = table.InternalForces;
        _currentForces = f;
        NtValue = Format(f.Nt);
        NcValue = Format(f.Nc);
        NValue = Format(f.N);
        QyValue = Format(f.Qy);
        QzValue = Format(f.Qz);
        MxValue = Format(f.Mx);
        MyValue = Format(f.My);
        MzValue = Format(f.Mz);
        MwValue = Format(f.Mw);
        MnegValue = Format(f.Mneg);

        // График несущей способности
        UpdateCapacityChart(f, _currentActual);

        // Жёсткость
        var s = table.Stiffness;
        SjValue = Format(s.Sj);
        SjoValue = Format(s.Sjo);

        // Коэффициенты
        var c = table.Coefficients;
        AlphaValue = Format(c.Alpha);
        BetaValue = Format(c.Beta);
        GammaValue = Format(c.Gamma);
        DeltaValue = Format(c.Delta);
        EpsilonValue = Format(c.Epsilon);
        LambdaValue = Format(c.Lambda);

        // Геометрия балки
        var beam = table.Geometry.Beam;
        BeamProfile = beam.ProfileBeam;
        BeamH = Format(beam.Beam_H);
        BeamB = Format(beam.Beam_B);
        BeamS = Format(beam.Beam_s);
        BeamT = Format(beam.Beam_t);
        BeamA = Format(beam.Beam_A);
        BeamP = Format(beam.Beam_P);
        BeamIz = Format(beam.Beam_Iz);
        BeamIy = Format(beam.Beam_Iy);
        BeamIx = Format(beam.Beam_Ix);
        BeamWz = Format(beam.Beam_Wz);
        BeamWy = Format(beam.Beam_Wy);
        BeamWx = Format(beam.Beam_Wx);
        BeamSz = Format(beam.Beam_Sz);
        BeamSy = Format(beam.Beam_Sy);
        Beamiz = Format(beam.Beam_iz);
        Beamiy = Format(beam.Beam_iy);
        BeamXo = Format(beam.Beam_xo);

        // Геометрия колонны
        var col = table.Geometry.Column;
        ColumnProfile = col.ProfileColumn;
        ColumnH = Format(col.Column_H);
        ColumnB = Format(col.Column_B);
        ColumnS = Format(col.Column_s);
        ColumnT = Format(col.Column_t);
        ColumnA = Format(col.Column_A);
        ColumnP = Format(col.Column_P);
        ColumnIz = Format(col.Column_Iz);
        ColumnIy = Format(col.Column_Iy);
        ColumnIx = Format(col.Column_Ix);
        ColumnWz = Format(col.Column_Wz);
        ColumnWy = Format(col.Column_Wy);
        ColumnWx = Format(col.Column_Wx);
        ColumnSz = Format(col.Column_Sz);
        ColumnSy = Format(col.Column_Sy);
        Columniz = Format(col.Column_iz);
        Columniy = Format(col.Column_iy);
        ColumnXo = Format(col.Column_xo);
        ColumnYo = Format(col.Column_yo);

        // Пластина
        var plate = table.Geometry.Plate;
        PlateH = Format(plate.Plate_H);
        PlateB = Format(plate.Plate_B);
        PlateT = Format(plate.Plate_t);

        // Фланец
        var flange = table.Geometry.Flange;
        FlangeH = Format(flange.Flange_H);
        FlangeB = Format(flange.Flange_B);
        FlangeT = Format(flange.Flange_t);
        FlangeLb = Format(flange.Flange_Lb);

        // Рёбра жёсткости
        var stiff = table.Geometry.Stiff;
        StiffTr1 = Format(stiff.Stiff_tr1);
        StiffTr2 = Format(stiff.Stiff_tr2);
        StiffTbp = Format(stiff.Stiff_tbp);
        StiffTg = Format(stiff.Stiff_tg);
        StiffTf = Format(stiff.Stiff_tf);
        StiffLh = Format(stiff.Stiff_Lh);
        StiffHh = Format(stiff.Stiff_Hh);
        StiffTwp = Format(stiff.Stiff_twp);

        // Болты
        var bolts = table.Bolts;
        BoltCount = bolts.CountBolt.Bolts_Nb.ToString();
        BoltRows = bolts.BoltRow.N_Rows.ToString();
        BoltDiameter = Format(bolts.DiameterBolt.F);
        BoltVersion = bolts.Option.version.ToString();
        BoltCoordZ = Format(bolts.CoordinatesBolts.Z.BoltCoordinateZ);
        var cy = bolts.CoordinatesBolts.Y;
        BoltE1 = Format(cy.Bolt1_e1);
        BoltP1 = Format(cy.Bolt2_p1);
        BoltP2 = Format(cy.Bolt3_p2);
        BoltP3 = Format(cy.Bolt4_p3);
        BoltP4 = Format(cy.Bolt5_p4);
        BoltP5 = Format(cy.Bolt6_p5);
        BoltP6 = Format(cy.Bolt7_p6);
        BoltP7 = Format(cy.Bolt8_p7);
        BoltP8 = Format(cy.Bolt9_p8);
        BoltP9 = Format(cy.Bolt10_p9);
        BoltP10 = Format(cy.Bolt11_p10);
        var cx = bolts.CoordinatesBolts.X;
        BoltD1 = Format(cx.d1);
        BoltD2 = Format(cx.d2);

        // Сварка
        var w = table.Welds;
        WeldKf1 = Format(w.kf1);
        WeldKf2 = Format(w.kf2);
        WeldKf3 = Format(w.kf3);
        WeldKf4 = Format(w.kf4);
        WeldKf5 = Format(w.kf5);
        WeldKf6 = Format(w.kf6);
        WeldKf7 = Format(w.kf7);
        WeldKf8 = Format(w.kf8);
        WeldKf9 = Format(w.kf9);
        WeldKf10 = Format(w.kf10);
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

    private void UpdateCapacityChart(InternalForcesData f, Rs1SummaryForces? actual = null)
    {
        var forceColor = Color.FromArgb("#2B579A");   // Primary — силы
        var momentColor = Color.FromArgb("#217346");  // Accent — моменты

        var items = new List<CapacityBarItem>
        {
            new("Nt, кН", f.Nt, forceColor, actual?.Nt),
            new("Nc, кН", f.Nc, forceColor, actual?.Nc),
            new("N, кН", f.N, forceColor, actual?.N),
            new("Qy, кН", f.Qy, forceColor, actual?.Qy),
            new("Qz, кН", f.Qz, forceColor, actual?.Qz),
            new("Mx, кН·м", f.Mx, momentColor, actual?.Mx),
            new("My, кН·м", f.My, momentColor, actual?.My),
            new("Mz, кН·м", f.Mz, momentColor, actual?.Mz),
            new("Mw, кН·м²", f.Mw, momentColor, actual?.Mw),
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
        UpdateCapacityChart(_currentForces, actual);
    }

    private static string Format(double value) => value == 0 ? "—" : value.ToString("G6");
}
