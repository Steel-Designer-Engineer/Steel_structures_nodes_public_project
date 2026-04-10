using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OfficeOpenXml;
using steel_structures_nodes.Calculate.Calculate;
using steel_structures_nodes.Calculate.Models;
using steel_structures_nodes.Calculate.Models.RSU;
using steel_structures_nodes.Calculate.Models.RSN;
using steel_structures_nodes.Calculate.Services;
using steel_structures_nodes.Domain.Entities;
using Microsoft.Extensions.Logging;
using steel_structures_nodes.Domain.Contracts;

namespace steel_structures_nodes.Maui.ViewModels;

public partial class ExcelImportViewModel : ObservableObject
{
    private readonly ICalculationResultRepository? _resultRepository;
    private readonly ILogger<ExcelImportViewModel>? _logger;

    public ExcelImportViewModel() { }

    public ExcelImportViewModel(
        ICalculationResultRepository resultRepository,
        ILogger<ExcelImportViewModel> logger)
    {
        _resultRepository = resultRepository;
        _logger = logger;
    }

    /// <summary>
    /// Вызывается после расчёта РС1 с фактическими максимальными усилиями.
    /// </summary>
    public event Action<Rs1SummaryForces>? Rs1ResultsUpdated;

    [ObservableProperty]
    public partial string? Status { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasRsuData { get; set; }

    [ObservableProperty]
    public partial bool HasRsnData { get; set; }

    [ObservableProperty]
    public partial bool HasResults { get; set; }

    [ObservableProperty]
    public partial bool HasAnalysisData { get; set; }

    [ObservableProperty]
    public partial string? ElementFilterText { get; set; }

    [ObservableProperty]
    public partial string? SelectedCalculationVersion { get; set; }

    private string _gammaF = "1";
    public string GammaF
    {
        get => _gammaF;
        set
        {
            if (string.Equals(_gammaF, value, StringComparison.Ordinal)) return;
            _gammaF = value;
            OnPropertyChanged();
            // Пересчитать автоматически, если данные уже загружены (как в WPF)
            if (HasRsuData || HasRsnData)
                _ = ExecuteCalculationAsync();
        }
    }

    public ObservableCollection<string> GammaFOptions { get; } = new() { "1", "1.05", "1.1", "1.15", "1.2", "1.3" };
    public ObservableCollection<string> CalculationVersions { get; } = new();

    public ObservableCollection<ForceRowDisplay> RsuRows { get; } = new();
    public ObservableCollection<ForceRowDisplay> RsnRows { get; } = new();
    public ObservableCollection<ResultItem> Results { get; } = new();
    public ObservableCollection<AnalysisRowDisplay> AnalysisRows { get; } = new();
    public ObservableCollection<IdeaRowDisplay> IdeaRows { get; } = new();

    [ObservableProperty]
    public partial bool HasIdeaData { get; set; }

    public string RsuSummaryTitle => $"Данные РСУ ({RsuRows.Count} строк)";
    public string RsnSummaryTitle => $"Данные РСН ({RsnRows.Count} строк)";

    public ObservableCollection<ForceRowDisplay> RsuPreviewRows { get; } = new();
    public ObservableCollection<ForceRowDisplay> RsnPreviewRows { get; } = new();

    private void UpdatePreviewRows(bool isRsu)
    {
        var source = isRsu ? RsuRows : RsnRows;
        var target = isRsu ? RsuPreviewRows : RsnPreviewRows;
        target.Clear();

        if (source.Count == 0) return;

        if (source.Count <= 3)
        {
            foreach (var row in source)
                target.Add(row);
            return;
        }

        target.Add(source[0]);
        target.Add(source[1]);
        target.Add(new ForceRowDisplay { DclNo = "...", N = "..." });
        target.Add(source[^1]);
    }

    [RelayCommand]
    private async Task ImportRsuAsync() => await ImportFromExcelAsync(isRsu: true);

    [RelayCommand]
    private async Task ImportRsnAsync() => await ImportFromExcelAsync(isRsu: false);

    [RelayCommand]
    private async Task ExecuteCalculationAsync()
    {
        try
        {
            IsBusy = true;
            Status = "Расчёт...";

            var rsuData = RsuRows.ToList();
            var rsnData = RsnRows.ToList();

            if (rsuData.Count == 0 && rsnData.Count == 0)
            {
                Status = "Нет данных РСУ/РСН для расчёта";
                return;
            }

            var gf = ParseCoeff(_gammaF);
            var elemFilterText = ElementFilterText;
            var resultDir = GetResultDir();

            var (resultItems, analysisItems, count, createdFileName, summaryForces, maxU, summaryPsi) = await Task.Run(() =>
            {
                var calc = new Calculator();

                var rsu = rsuData.Select(r => (ForceRow)new RsuRow
                {
                    DclNo = r.DclNo, Elem = r.Elem, Sect = r.Sect,
                    N = r.N, Mx = r.Mx, My = r.My, Qz = r.Qz, Mz = r.Mz, Qy = r.Qy, Mw = r.Mw
                }).ToList();

                var rsn = rsnData.Select(r => (ForceRow)new RsnRow
                {
                    DclNo = r.DclNo, Elem = r.Elem, Sect = r.Sect,
                    N = r.N, Mx = r.Mx, My = r.My, Qz = r.Qz, Mz = r.Mz, Qy = r.Qy, Mw = r.Mw
                }).ToList();

                if (gf != 1d)
                {
                    rsu = ScaleForceRows(rsu, gf);
                    rsn = ScaleForceRows(rsn, gf);
                }

                HashSet<string>? elemFilter = null;
                if (!string.IsNullOrWhiteSpace(elemFilterText))
                    elemFilter = ParseElementFilter(elemFilterText);

                Directory.CreateDirectory(resultDir);
                calc.CalculateAndSave(rsu, rsn, elemFilter, resultDir, out var createdFilePath);

                var json = File.ReadAllText(createdFilePath, System.Text.Encoding.UTF8);
                var result = Rs1ResultJsonSerializer.FromJson(json);

                var items = new List<ResultItem>
                {
                    new("N (кН)", result.SummaryN),
                    new("Nt (кН)", result.SummaryNt),
                    new("Nc (кН)", result.SummaryNc),
                    new("Qy (кН)", result.SummaryQy),
                    new("Qz (кН)", result.SummaryQz),
                    new("Mz (кН·м)", result.SummaryMz),
                    new("Mx (кН·м)", result.SummaryMx),
                    new("My (кН·м)", result.SummaryMy),
                    new("Mw (кН*м?)", result.SummaryMw),
                    new("u (макс.)", result.MaxU),
                    new("?", result.SummaryPsi),
                };

                var aRows = new List<AnalysisRowDisplay>();
                if (result.AnalysisRows != null)
                {
                    foreach (var r in result.AnalysisRows)
                    {
                        if (r == null) continue;
                        aRows.Add(new AnalysisRowDisplay
                        {
                            RowType = r.RowType,
                            LoadCombination = r.LoadCombination,
                            Element = r.Element,
                            N = r.N, Nt = r.Nt, Nc = r.Nc,
                            Qy = r.Qy, Qz = r.Qz,
                            Mx = r.Mx, My = r.My, Mz = r.Mz, Mw = r.Mw,
                            U = r.U, Psi = r.Psi,
                        });
                    }
                }

                return (items, aRows, rsu.Count + rsn.Count, Path.GetFileName(createdFilePath),
                    new Rs1SummaryForces(
                        result.SummaryN, result.SummaryNt, result.SummaryNc,
                        result.SummaryQy, result.SummaryQz,
                        result.SummaryMx, result.SummaryMy, result.SummaryMz, result.SummaryMw),
                    result.MaxU, result.SummaryPsi);
            });

            Results.Clear();
            foreach (var item in resultItems)
                Results.Add(item);

            AnalysisRows.Clear();
            foreach (var row in analysisItems)
                AnalysisRows.Add(row);

            HasResults = true;
            HasAnalysisData = AnalysisRows.Count > 0;
            BuildIdeaStaticaTable();

            Rs1ResultsUpdated?.Invoke(summaryForces);

            // Сохранить результат в MongoDB (коллекция Result)
            await SaveResultToMongoAsync(summaryForces, maxU, summaryPsi,
                analysisItems, rsuData.Count, rsnData.Count, gf, elemFilterText);

            var parts = new List<string>();
            if (rsuData.Count > 0) parts.Add($"РСУ={rsuData.Count}");
            if (rsnData.Count > 0) parts.Add($"РСН={rsnData.Count}");
            Status = $"Расчёт выполнен ({string.Join(", ", parts)}), сохранён в {createdFileName}";
        }
        catch (Exception ex)
        {
            Status = $"Ошибка расчёта: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Calculation error: {ex}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveResultToMongoAsync(
        Rs1SummaryForces forces, double? maxU, double? psi,
        List<AnalysisRowDisplay> analysisItems,
        int rsuCount, int rsnCount,
        double gammaF, string? elementFilter)
    {
        if (_resultRepository is null) return;

        try
        {
            var entity = new CalculationResult
            {
                CreatedAtUtc = DateTime.UtcNow,
                GammaF = gammaF,
                ElementFilter = elementFilter,
                RsuCount = rsuCount,
                RsnCount = rsnCount,
                SummaryN = forces.N,
                SummaryNt = forces.Nt,
                SummaryNc = forces.Nc,
                SummaryQy = forces.Qy,
                SummaryQz = forces.Qz,
                SummaryMx = forces.Mx,
                SummaryMy = forces.My,
                SummaryMz = forces.Mz,
                SummaryMw = forces.Mw,
                MaxU = maxU,
                SummaryPsi = psi,
                AnalysisRows = analysisItems.Select(a => new CalculationResultAnalysisRow
                {
                    RowType = a.RowType,
                    LoadCombination = a.LoadCombination,
                    Element = a.Element,
                    N = a.N, Nt = a.Nt, Nc = a.Nc,
                    Qy = a.Qy, Qz = a.Qz,
                    Mx = a.Mx, My = a.My, Mz = a.Mz, Mw = a.Mw,
                    U = a.U, Psi = a.Psi,
                }).ToList(),
            };

            await _resultRepository.AddAsync(entity);
            _logger?.LogInformation("Результат расчёта сохранён в MongoDB: {Id}", entity.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Не удалось сохранить результат в MongoDB");
            System.Diagnostics.Debug.WriteLine($"MongoDB save error: {ex.Message}");
        }
    }

    /// <summary>
    /// Строит таблицу нагрузок в нотации IDEA StatiCA из таблицы анализа.
    /// Маппинг: Qy>Vy, Qz>Vz, Mx>Mx, My>My, Mz>Mz (идентично WPF BuildIdeaStaticaTable).
    /// </summary>
    private void BuildIdeaStaticaTable()
    {
        IdeaRows.Clear();
        foreach (var a in AnalysisRows)
        {
            IdeaRows.Add(new IdeaRowDisplay
            {
                RowType = MapRowTypeToIdea(a.RowType),
                N  = a.N,
                Vy = a.Qy,
                Vz = a.Qz,
                Mx = a.Mx,
                My = a.My,
                Mz = a.Mz,
            });
        }
        HasIdeaData = IdeaRows.Count > 0;
    }

    /// <summary>
    /// Маппинг наименований строк анализа в нотацию IDEA StatiCA (идентично WPF MapRowTypeToIdea).
    /// </summary>
    private static string? MapRowTypeToIdea(string? rowType)
    {
        if (string.IsNullOrWhiteSpace(rowType)) return rowType;
        var rt = rowType.Trim();
        if (rt.Equals("MAX Qy", StringComparison.OrdinalIgnoreCase)) return "MAX Qo";
        if (rt.Equals("MAX Qz", StringComparison.OrdinalIgnoreCase)) return "MAX Q";
        if (rt.Equals("MAX Mx", StringComparison.OrdinalIgnoreCase)) return "MAX T";
        if (rt.Equals("MAX My", StringComparison.OrdinalIgnoreCase)) return "MAX M";
        if (rt.Equals("MAX Mz", StringComparison.OrdinalIgnoreCase)) return "MAX Mo";
        if (rt.Equals("MAX N+", StringComparison.OrdinalIgnoreCase)) return "MAX Nt";
        if (rt.Equals("MAX N-", StringComparison.OrdinalIgnoreCase)) return "MAX Nc";
        return rt;
    }

    [RelayCommand]
    private void LoadSelectedVersion()
    {
        if (string.IsNullOrWhiteSpace(SelectedCalculationVersion)) return;
        var path = Path.Combine(GetResultDir(), SelectedCalculationVersion + ".json");
        if (!File.Exists(path)) { Status = "Файл не найден"; return; }

        var json = File.ReadAllText(path, System.Text.Encoding.UTF8);
        var result = Rs1ResultJsonSerializer.FromJson(json);

        Results.Clear();
        Results.Add(new ResultItem("N (кН)", result.SummaryN));
        Results.Add(new ResultItem("Nt (кН)", result.SummaryNt));
        Results.Add(new ResultItem("Nc (кН)", result.SummaryNc));
        Results.Add(new ResultItem("Qy (кН)", result.SummaryQy));
        Results.Add(new ResultItem("Qz (кН)", result.SummaryQz));
        Results.Add(new ResultItem("Mz (кН·м)", result.SummaryMz));
        Results.Add(new ResultItem("Mx (кН·м)", result.SummaryMx));
        Results.Add(new ResultItem("My (кН·м)", result.SummaryMy));
        Results.Add(new ResultItem("Mw (кН*м?)", result.SummaryMw));
        Results.Add(new ResultItem("u (макс.)", result.MaxU));
        Results.Add(new ResultItem("?", result.SummaryPsi));

        AnalysisRows.Clear();
        if (result.AnalysisRows != null)
        {
            foreach (var r in result.AnalysisRows)
            {
                if (r == null) continue;
                AnalysisRows.Add(new AnalysisRowDisplay
                {
                    RowType = r.RowType,
                    LoadCombination = r.LoadCombination,
                    Element = r.Element,
                    N = r.N, Nt = r.Nt, Nc = r.Nc,
                    Qy = r.Qy, Qz = r.Qz,
                    Mx = r.Mx, My = r.My, Mz = r.Mz, Mw = r.Mw,
                    U = r.U, Psi = r.Psi,
                });
            }
        }

        HasResults = true;
        HasAnalysisData = AnalysisRows.Count > 0;
        BuildIdeaStaticaTable();

        Rs1ResultsUpdated?.Invoke(new Rs1SummaryForces(
            result.SummaryN, result.SummaryNt, result.SummaryNc,
            result.SummaryQy, result.SummaryQz,
            result.SummaryMx, result.SummaryMy, result.SummaryMz, result.SummaryMw));

        Status = $"Загружена версия: {SelectedCalculationVersion}";
    }

    private async Task ImportFromExcelAsync(bool isRsu)
    {
        try
        {
            IsBusy = true;
            Status = "Выбор файла...";

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = isRsu ? "Выберите Excel-файл РСУ" : "Выберите Excel-файл РСН",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".xlsx", ".xlsm", ".xls" } },
                    { DevicePlatform.Android, new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/vnd.ms-excel" } }
                })
            });

            if (result == null)
            {
                Status = "Отменено";
                return;
            }

            ExcelPackage.License.SetNonCommercialPersonal("steel_structures_nodes");

            // На Android файл может быть доступен только через поток
            using var stream = await result.OpenReadAsync();
            using var package = new ExcelPackage(stream);
            var sheetNames = package.Workbook.Worksheets.Select(w => w.Name).ToList();

            if (sheetNames.Count == 0)
            {
                Status = "Листы не найдены в файле";
                return;
            }

            string sheetName;
            if (sheetNames.Count == 1)
            {
                sheetName = sheetNames[0];
            }
            else
            {
                var page = Application.Current?.Windows.FirstOrDefault()?.Page;
                sheetName = page != null
                    ? await page.DisplayActionSheetAsync("Выберите лист", "Отмена", null, sheetNames.ToArray()) ?? ""
                    : sheetNames[0];

                if (string.IsNullOrWhiteSpace(sheetName) || sheetName == "Отмена")
                {
                    Status = "Отменено";
                    return;
                }
            }

            Status = $"Чтение листа '{sheetName}'...";
            var ws = package.Workbook.Worksheets[sheetName];
            var dim = ws?.Dimension;

            if (ws == null || dim == null)
            {
                Status = "Лист пуст";
                return;
            }

            var target = isRsu ? RsuRows : RsnRows;
            target.Clear();

            int colCount = dim.End.Column;
            int rowCount = dim.End.Row;

            // Find best header row (scan up to 80 rows)
            int headerRow = FindBestHeaderRow(ws, Math.Min(80, rowCount), colCount);
            var headerMap = BuildHeaderMap(ws, headerRow, colCount);

            // Detect RSU by "ЗАГРУЖЕНИЯ" header
            bool detectedRsu = TryHeaderMatch(headerMap, "ЗАГРУЖЕНИЯ") > 0;

            // Map columns using prefix matching and fallbacks (same as WPF reader)
            int cDcl = FirstPositive(
                TryHeaderMatch(headerMap, "ЗАГРУЖЕНИЯ"),
                TryHeaderMatch(headerMap, "Номер РСН"),
                TryHeaderMatch(headerMap, "DCL No"),
                TryHeaderMatch(headerMap, "DCLNo"),
                TryHeaderMatch(headerMap, "Номер РСУ"));

            int cElem = FirstPositive(
                TryHeaderMatch(headerMap, "ЭЛЕМ"),
                TryHeaderMatch(headerMap, "ELEM"),
                TryHeaderMatch(headerMap, "Элемент"),
                TryHeaderMatch(headerMap, "Element"));

            int cSect = FirstPositive(
                TryHeaderMatch(headerMap, "SECT"),
                TryHeaderMatch(headerMap, "СЕЧ"),
                TryHeaderMatch(headerMap, "СЕЧ."),
                TryHeaderMatch(headerMap, "НС"),
                TryHeaderMatch(headerMap, "Сечение"),
                TryHeaderMatch(headerMap, "Section"));

            int cN = FirstPositive(TryPrefixMatch(headerMap, "N"), TryHeaderMatch(headerMap, "N"));
            int cMx = FirstPositive(TryPrefixMatch(headerMap, "MK"), TryPrefixMatch(headerMap, "MX"), TryHeaderMatch(headerMap, "MK"));
            int cMy = FirstPositive(TryPrefixMatch(headerMap, "MY"), TryHeaderMatch(headerMap, "MY"));
            int cQz = FirstPositive(TryPrefixMatch(headerMap, "QZ"), TryHeaderMatch(headerMap, "QZ"));
            int cMz = FirstPositive(TryPrefixMatch(headerMap, "MZ"), TryHeaderMatch(headerMap, "MZ"));
            int cQy = FirstPositive(TryPrefixMatch(headerMap, "QY"), TryHeaderMatch(headerMap, "QY"));
            int cMw = FirstPositive(TryPrefixMatch(headerMap, "MW"), TryHeaderMatch(headerMap, "MW"));

            for (int r = headerRow + 1; r <= rowCount; r++)
            {
                string CellVal(int col)
                {
                    if (col <= 0) return "";
                    var v = ws.Cells[r, col].Value;
                    return v == null ? "" : Convert.ToString(v)!.Trim();
                }

                var dclRaw = CellVal(cDcl);
                var row = new ForceRowDisplay
                {
                    DclNo = dclRaw,
                    Elem = CellVal(cElem),
                    Sect = CellVal(cSect),
                    N = NormalizeNumber(CellVal(cN)),
                    Mx = NormalizeNumber(CellVal(cMx)),
                    My = NormalizeNumber(CellVal(cMy)),
                    Qz = NormalizeNumber(CellVal(cQz)),
                    Mz = NormalizeNumber(CellVal(cMz)),
                    Qy = NormalizeNumber(CellVal(cQy)),
                    Mw = NormalizeNumber(CellVal(cMw))
                };

                if (string.IsNullOrWhiteSpace(row.N) && string.IsNullOrWhiteSpace(row.Mx) &&
                    string.IsNullOrWhiteSpace(row.My) && string.IsNullOrWhiteSpace(row.DclNo))
                    continue;

                target.Add(row);
            }

            if (isRsu) HasRsuData = target.Count > 0;
            else HasRsnData = target.Count > 0;

            UpdatePreviewRows(isRsu);
            OnPropertyChanged(isRsu ? nameof(RsuSummaryTitle) : nameof(RsnSummaryTitle));

            Status = $"{(isRsu ? "РСУ" : "РСН")} импортировано: {target.Count} строк (заголовок в строке {headerRow})";
        }
        catch (Exception ex)
        {
            Status = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

private static double ParseCoeff(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 1d;
        var t = s.Trim().Replace(',', '.');
        if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            return d;
        return 1d;
    }

    private static List<ForceRow> ScaleForceRows(List<ForceRow> rows, double gf)
    {
        var result = new List<ForceRow>(rows.Count);
        foreach (var r in rows)
        {
            result.Add(new ForceRow
            {
                DclNo = r.DclNo,
                Elem = r.Elem,
                Sect = r.Sect,
                N  = ScaleValue(r.N,  gf),
                Mx = ScaleValue(r.Mx, gf),
                My = ScaleValue(r.My, gf),
                Qz = ScaleValue(r.Qz, gf),
                Mz = ScaleValue(r.Mz, gf),
                Qy = ScaleValue(r.Qy, gf),
                Mw = ScaleValue(r.Mw, gf),
            });
        }
        return result;
    }

    private static string ScaleValue(string s, double k)
    {
        if (k == 1d || string.IsNullOrWhiteSpace(s)) return s;
        var t = s.Trim().Replace(',', '.');
        if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            return (d * k).ToString("G", CultureInfo.InvariantCulture);
        return s;
    }

    private static HashSet<string> ParseElementFilter(string text)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text)) return set;

        var tokens = text.Replace(',', ' ').Replace(';', ' ')
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var t in tokens)
        {
            var tok = t.Trim();
            if (tok.Length == 0) continue;

            var dots = tok.IndexOf("...", StringComparison.Ordinal);
            if (dots > 0
                && int.TryParse(tok.Substring(0, dots), NumberStyles.Integer, CultureInfo.InvariantCulture, out var a1)
                && int.TryParse(tok.Substring(dots + 3), NumberStyles.Integer, CultureInfo.InvariantCulture, out var b1))
            {
                for (int i = Math.Min(a1, b1); i <= Math.Max(a1, b1); i++)
                    set.Add(i.ToString(CultureInfo.InvariantCulture));
                continue;
            }

            var dash = tok.IndexOf('-');
            if (dash > 0 && dash < tok.Length - 1
                && int.TryParse(tok.Substring(0, dash), NumberStyles.Integer, CultureInfo.InvariantCulture, out var a2)
                && int.TryParse(tok.Substring(dash + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var b2))
            {
                for (int i = Math.Min(a2, b2); i <= Math.Max(a2, b2); i++)
                    set.Add(i.ToString(CultureInfo.InvariantCulture));
                continue;
            }

            if (int.TryParse(tok, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            {
                set.Add(n.ToString(CultureInfo.InvariantCulture));
                continue;
            }

            set.Add(tok);
        }

        return set;
    }

    private static string GetResultDir()
    {
        // На мобильных платформах используем AppDataDirectory (гарантированно записываемая директория).
        // На Windows пробуем найти папку проекта рядом с exe (для разработки).
        string baseDir;
        try
        {
            baseDir = FileSystem.AppDataDirectory;
        }
        catch
        {
            baseDir = AppDomain.CurrentDomain.BaseDirectory;
        }

        if (!string.IsNullOrWhiteSpace(baseDir))
        {
            var dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 10 && dir != null; i++)
            {
                var projectDir = Path.Combine(dir.FullName, "steel_structures_nodes.Calculate");
                if (Directory.Exists(projectDir))
                {
                    var candidate = Path.Combine(projectDir, "ResultCalculate");
                    Directory.CreateDirectory(candidate);
                    return candidate;
                }
                dir = dir.Parent;
            }
        }

        var fallback = Path.Combine(baseDir ?? FileSystem.AppDataDirectory, "ResultCalculate");
        Directory.CreateDirectory(fallback);
        return fallback;
    }

    private static List<string> GetResultFiles()
    {
        var resultDir = GetResultDir();
        if (!Directory.Exists(resultDir))
            return new List<string>();

        return Directory.GetFiles(resultDir, "Result_v*.json")
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void RebuildCalculationVersions()
    {
        CalculationVersions.Clear();
        foreach (var f in GetResultFiles())
            CalculationVersions.Add(Path.GetFileNameWithoutExtension(f));

        if (CalculationVersions.Count > 0)
            SelectedCalculationVersion = CalculationVersions[CalculationVersions.Count - 1];
    }

    /// <summary>
    /// Scans up to <paramref name="maxRows"/> rows to find the row that best matches
    /// known RSU/RSN header keywords (same heuristic as WPF EpplusExcelReader).
    /// </summary>
    private static int FindBestHeaderRow(ExcelWorksheet ws, int maxRows, int colCount)
    {
        int bestRow = 1;
        int bestScore = -1;

        for (int r = 1; r <= maxRows; r++)
        {
            var map = BuildHeaderMap(ws, r, colCount);

            int score = 0;
            if (TryHeaderMatch(map, "DCL No") > 0 || TryHeaderMatch(map, "ЗАГРУЖЕНИЯ") > 0 || TryHeaderMatch(map, "Номер РСН") > 0) score++;
            if (TryHeaderMatch(map, "ЭЛЕМ") > 0 || TryHeaderMatch(map, "ELEM") > 0) score++;
            if (TryPrefixMatch(map, "N") > 0) score++;
            if (TryPrefixMatch(map, "MK") > 0) score++;
            if (TryPrefixMatch(map, "QZ") > 0) score++;
            if (TryPrefixMatch(map, "MZ") > 0) score++;
            if (TryPrefixMatch(map, "QY") > 0) score++;

            if (score > bestScore)
            {
                bestScore = score;
                bestRow = r;
            }
        }

        return bestRow;
    }

    private static Dictionary<string, int> BuildHeaderMap(ExcelWorksheet ws, int headerRow, int colCount)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int c = 1; c <= colCount; c++)
        {
            var v = ws.Cells[headerRow, c].Value;
            if (v == null) continue;
            var key = NormalizeHeader(Convert.ToString(v)!);
            if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
                map[key] = c;
        }
        return map;
    }

    private static int TryHeaderMatch(Dictionary<string, int> map, string header)
    {
        var key = NormalizeHeader(header);
        if (map.TryGetValue(key, out var idx))
            return idx;

        // Try alternative separators
        var alternatives = new[]
        {
            header.Replace(',', '.'),
            header.Replace('.', ','),
            header.Replace("*", "?"),
            header.Replace("?", "*")
        };

        foreach (var alt in alternatives)
        {
            var altKey = NormalizeHeader(alt);
            if (map.TryGetValue(altKey, out idx))
                return idx;
        }

        return -1;
    }

    private static int TryPrefixMatch(Dictionary<string, int> map, string prefix)
    {
        var p = NormalizeHeader(prefix);
        foreach (var kv in map)
        {
            if (kv.Key.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        }
        return -1;
    }

    private static int FirstPositive(params int[] values)
    {
        foreach (var v in values)
            if (v > 0) return v;
        return 0;
    }

    private static string NormalizeHeader(string s)
    {
        var t = (s ?? "").Trim()
            .Replace(" ", "")
            .Replace("\t", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("?", "*");
        return t;
    }

    private static string NormalizeNumber(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        return s.Trim().Replace(',', '.');
    }
}

public class ForceRowDisplay
{
    public string DclNo { get; set; } = "";
    public string Elem { get; set; } = "";
    public string Sect { get; set; } = "";
    public string N { get; set; } = "";
    public string Mx { get; set; } = "";
    public string My { get; set; } = "";
    public string Qz { get; set; } = "";
    public string Mz { get; set; } = "";
    public string Qy { get; set; } = "";
    public string Mw { get; set; } = "";
}

public class ResultItem
{
    public ResultItem(string key, double? value)
    {
        Key = key;
        Value = FormatOrDash(value);
    }

    public string Key { get; }
    public string Value { get; }

    private static string FormatOrDash(double? v)
    {
        if (!v.HasValue) return "—";
        return v.Value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}

public class AnalysisRowDisplay
{
    public string? RowType { get; set; }
    public string? LoadCombination { get; set; }
    public int? Element { get; set; }
    public double? N { get; set; }
    public double? Nt { get; set; }
    public double? Nc { get; set; }
    public double? Qy { get; set; }
    public double? Qz { get; set; }
    public double? Mx { get; set; }
    public double? My { get; set; }
    public double? Mz { get; set; }
    public double? Mw { get; set; }
    public double? U { get; set; }
    public double? Psi { get; set; }

    public string ElementText => Element.HasValue ? Element.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;

    public string NText => FormatOrDash(N);
    public string NtText => FormatOrDash(Nt);
    public string NcText => FormatOrDash(Nc);
    public string QyText => FormatOrDash(Qy);
    public string QzText => FormatOrDash(Qz);
    public string MxText => FormatOrDash(Mx);
    public string MyText => FormatOrDash(My);
    public string MzText => FormatOrDash(Mz);
    public string MwText => FormatOrDash(Mw);
    public string UText => FormatOrDash(U);
    public string PsiText => FormatOrDash(Psi);

    private static string FormatOrDash(double? v)
    {
        if (!v.HasValue) return "—";
        return v.Value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// Строка таблицы нагрузок для IDEA StatiCA (идентична WPF IdeaStaticaRowViewModel).
/// </summary>
public class IdeaRowDisplay
{
    /// <summary>Критерий (MAX Coeff, MAX Vy, MAX Vz, …).</summary>
    public string? RowType { get; set; }

    /// <summary>Продольная сила N (кН).</summary>
    public double? N { get; set; }

    /// <summary>Поперечная сила Vy (кН) — соответствует Qy в Лира / Qo в Альбоме.</summary>
    public double? Vy { get; set; }

    /// <summary>Поперечная сила Vz (кН) — соответствует Qz в Лира / Q в Альбоме.</summary>
    public double? Vz { get; set; }

    /// <summary>Крутящий момент Mx (кН·м) — соответствует Mx/MK в Лира / T в Альбоме.</summary>
    public double? Mx { get; set; }

    /// <summary>Изгибающий момент My (кН·м) — соответствует MY в Лира / M в Альбоме.</summary>
    public double? My { get; set; }

    /// <summary>Изгибающий момент Mz (кН·м) — соответствует MZ в Лира / Mo в Альбоме.</summary>
    public double? Mz { get; set; }

    public string NText => FormatOrDash(N);
    public string VyText => FormatOrDash(Vy);
    public string VzText => FormatOrDash(Vz);
    public string MxText => FormatOrDash(Mx);
    public string MyText => FormatOrDash(My);
    public string MzText => FormatOrDash(Mz);

    private static string FormatOrDash(double? v)
    {
        if (!v.HasValue) return "—";
        return v.Value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
