using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using steel_structures_nodes.Data.Contracts;
using steel_structures_nodes.Calculate.Models;
using steel_structures_nodes.Calculate.Services;
using steel_structures_nodes.Wpf.Mvvm;
using steel_structures_nodes.Wpf.Models;
using steel_structures_nodes.Wpf.Services;
using steel_structures_nodes.Calculate.Models.RSN;
using steel_structures_nodes.Calculate.Models.RSU;
using steel_structures_nodes.Domain.Contracts;

namespace steel_structures_nodes.Wpf.ViewModels
{
    /// <summary>
    /// Главная ViewModel расчёта РС1: импорт РСУ/РСН, вычисление усилий, отображение результатов и коэффициентов.
    /// </summary>
    public partial class ViewModel : ViewModelBase
    {
        private readonly IExcelImportDialogService _excelImportDialogService;
        private readonly steel_structures_nodes.Calculate.Services.IExcelReader _excelReader;
        private readonly IInteractionTableService _interactionService;
        private readonly ICalculationResultRepository _calculationResultRepository;
        private readonly IDataAccessFailureNotifier _dataAccessFailureNotifier;
        private readonly IWpfNodeImageService _nodeImageService;

        private string _status;
        private string _nodeTopology;
        private string _elementSectionBeam;// Элемент секции балки для отображения в превью и передачи в калькулятор (может быть пустым, если не задано в interaction_tables.json)
        private string _elementSectionColumn;// Элемент секции колонны для отображения в превью и передачи в калькулятор (может быть пустым, если не задано в interaction_tables.json)
        private string _connectionName;
        private string _standardConnectionCode;
        private string _elementFilterText;

        private ConnectionOptionViewModel _selectedConnectionOption;
        private ImageSource _selectedNodeImage;
        private string _selectedNodeDescription;
        private List<ImageSource> _nodeImages = new List<ImageSource>();
        private int _nodeImageIndex;

        private bool _isInitialized;
        private steel_structures_nodes.Wpf.Models.StandardNodeData _lastStandardNodeData;

        private double? _lastCalcNPlus;
        private double? _lastCalcNMinus;
        private double? _lastCalcQAbs;
        private double? _lastCalcQzAbs;
        private double? _lastCalcTAbs;
        private double? _lastCalcMAbs;
        private double? _lastCalcMoAbs;
        private double? _lastCalcMwAbs;
        private bool _hasCalcData;

        private string _userNt = string.Empty;
        private string _userNc = string.Empty;
        private string _userN = string.Empty;
        private string _userQy = string.Empty;
        private string _userQz = string.Empty;
        private string _userMx = string.Empty;
        private string _userMy = string.Empty;
        private string _userMz = string.Empty;
        private string _userMw = string.Empty;

        private string _gammaF = "1";
        private WindowState _windowState = WindowState.Normal;

        /// <summary>Вызывается после завершения расчёта для прокрутки UI к результатам.</summary>
        public event Action CalculationCompleted;

        /// <summary>Доступные значения коэффициента ?_f для выбора в ComboBox.</summary>
        public ObservableCollection<string> GammaFOptions { get; } = new ObservableCollection<string> { "1", "1.05", "1.1", "1.15", "1.2", "1.3" };

        /// <summary>
        /// Основной конструктор. Инициализирует все сервисы (Excel, MongoDB),
        /// создаёт команды UI, загружает начальные данные (имена узлов, профили)
        /// и применяет значения по умолчанию.
        /// </summary>
        public ViewModel(IExcelImportDialogService excelImportDialogService, steel_structures_nodes.Calculate.Services.IExcelReader excelReader, IInteractionTableService interactionService, ICalculationResultRepository calculationResultRepository, IDataAccessFailureNotifier dataAccessFailureNotifier, IWpfNodeImageService nodeImageService)
        {
            _excelImportDialogService = excelImportDialogService ?? throw new ArgumentNullException(nameof(excelImportDialogService));
            _excelReader = excelReader ?? throw new ArgumentNullException(nameof(excelReader));
            _interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
            _calculationResultRepository = calculationResultRepository ?? throw new ArgumentNullException(nameof(calculationResultRepository));
            _dataAccessFailureNotifier = dataAccessFailureNotifier ?? throw new ArgumentNullException(nameof(dataAccessFailureNotifier));
            _nodeImageService = nodeImageService;
            _dataAccessFailureNotifier.FailureOccurred += OnDataAccessFailure;

            if (_dataAccessFailureNotifier.LastFailure is not null)
                _status = _dataAccessFailureNotifier.LastFailure.Message;

            _isInitialized = false;

            ConnectionOptions = new ObservableCollection<ConnectionOptionViewModel>(ConnectionOptionLoader.LoadAll());

            // Name list from MongoDB (collection 'all_node')
            // If MongoDB is unreachable, start with empty list
            string[] names;
            try
            {
                names = _interactionService.LoadDistinctNames();
            }
            catch
            {
                names = Array.Empty<string>();
                _status = "Ошибка подключения к MongoDB";
            }
            ConnectionCodes = new ObservableCollection<string>(names);

            // Start with empty lists; CONNECTION_CODE and Profile lists will be derived from MongoDB for selected Name
            ConnectionCodeItems = new ObservableCollection<string>();
            ElementSectionsBeam = new ObservableCollection<string>();//Секции для балок
            ElementSectionsColumn = new ObservableCollection<string>();//Секции для колонн

            RsuRows = new ObservableCollection<RsuRow>();
            RsnRows = new ObservableCollection<RsnRow>();
            Results = new ObservableCollection<ResultItemViewModel>();
            InteractionResults = new ObservableCollection<ResultItemViewModel>();
            AnalysisRows = new ObservableCollection<Rs1AnalysisRowViewModel>();
            ComparisonItems = new ObservableCollection<ComparisonChartItem>();
            IdeaRows = new ObservableCollection<IdeaStaticaRowViewModel>();

            ClearDataCommand = new RelayCommand(ClearData);
            PasteRsuCommand = new RelayCommand(PasteRsu);
            PasteRsnCommand = new RelayCommand(PasteRsn);
            AddRsuCommand = new RelayCommand(AddRsu);
            AddRsnCommand = new RelayCommand(AddRsn);
            ExecuteCalculationCommand = new RelayCommand(ExecuteCalculation);

            ImportRsuFromExcelCommand = new RelayCommand(ImportRsuFromExcel);
            ImportRsnFromExcelCommand = new RelayCommand(ImportRsnFromExcel);

            PickElementSectionCommand = new RelayCommand(PickElementSection);
            PickConnectionNameCommand = new RelayCommand(PickConnectionName);
            CopyIdeaToClipboardCommand = new RelayCommand(CopyIdeaToClipboard);
            CopyAnalysisToClipboardCommand = new RelayCommand(CopyAnalysisToClipboard);
            CopyResultsToClipboardCommand = new RelayCommand(CopyResultsToClipboard);
            CopyNodeDataToClipboardCommand = new RelayCommand(CopyNodeDataToClipboard);
            PrevImageCommand = new RelayCommand(PrevImage, () => _nodeImages.Count > 1);
            NextImageCommand = new RelayCommand(NextImage, () => _nodeImages.Count > 1);
            MinimizeCommand = new RelayCommand(() => CurrentWindowState = WindowState.Minimized);
            MaximizeRestoreCommand = new RelayCommand(() =>
                CurrentWindowState = CurrentWindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized);
            CloseCommand = new RelayCommand(() => Application.Current?.MainWindow?.Close());

            StandardNode = new StandardNodeInteractionViewModel();

            _isInitialized = true;

            // Apply defaults: Name -> Profile -> CONNECTION_CODE (all from MongoDB)
            ConnectionName = ConnectionCodes.Count > 0 ? ConnectionCodes[0] : string.Empty;

            SelectedConnectionOption = ConnectionOptions.FirstOrDefault(x => string.Equals(x.Code, StandardConnectionCode, StringComparison.OrdinalIgnoreCase))
                                       ?? ConnectionOptions.FirstOrDefault();

            UpdateSelectedNodePresentation();
            UpdateStandardNodeFromJson();

            // Загрузка сохранённого ?f (глобальная константа решения)
            var savedGammaF = SolutionSettings.LoadGammaF();
            if (GammaFOptions.Contains(savedGammaF))
                _gammaF = savedGammaF;
            else
                _gammaF = GammaFOptions[0];
            OnPropertyChanged(nameof(GammaF));
        }

        private void OnDataAccessFailure(object sender, DataAccessFailureEventArgs e)
        {
            Status = e.Message;
        }

        /// <summary>Строки РСУ, загруженные пользователем (Excel/буфер обмена).</summary>
        public ObservableCollection<RsuRow> RsuRows { get; }

        /// <summary>Строки РСН, загруженные пользователем (Excel/буфер обмена).</summary>
        public ObservableCollection<RsnRow> RsnRows { get; }

        /// <summary>Сводка результатов расчёта (для отображения в UI).</summary>
        public ObservableCollection<ResultItemViewModel> Results { get; }

        /// <summary>Результаты проверки/таблиц взаимодействия (если используются в UI).</summary>
        public ObservableCollection<ResultItemViewModel> InteractionResults { get; }

        /// <summary>Таблица анализа (MAX-строки), полученная из JSON результата.</summary>
        public ObservableCollection<Rs1AnalysisRowViewModel> AnalysisRows { get; }

        /// <summary>Данные для сравнительной диаграммы (табличные vs расчётные).</summary>
        public ObservableCollection<ComparisonChartItem> ComparisonItems { get; }

        /// <summary>Таблица нагрузок в нотации IDEA StatiCA.</summary>
        public ObservableCollection<IdeaStaticaRowViewModel> IdeaRows { get; }

        public ObservableCollection<ConnectionOptionViewModel> ConnectionOptions { get; }

        public string NodeTopology
        {
            get => _nodeTopology;
            set
            {
                var v = (value ?? string.Empty).Trim();
                if (string.Equals(_nodeTopology, v, StringComparison.OrdinalIgnoreCase))
                    return;

                _nodeTopology = v;
                OnPropertyChanged();

                // keep SelectedConnectionOption in sync for UI only
                if (ConnectionOptions != null)
                {
                    var opt = ConnectionOptions.FirstOrDefault(x => string.Equals(x.Code, _nodeTopology, StringComparison.OrdinalIgnoreCase));
                    if (!ReferenceEquals(_selectedConnectionOption, opt))
                    {
                        _selectedConnectionOption = opt;
                        OnPropertyChanged(nameof(SelectedConnectionOption));
                    }
                }

                // Do NOT update preview here (preview follows NAME only)
            }
        }

        public string UserNt { get => _userNt; set { _userNt = value; OnPropertyChanged(); RebuildComparisonIfNeeded(); } }
        public string UserNc { get => _userNc; set { _userNc = value; OnPropertyChanged(); RebuildComparisonIfNeeded(); } }
        public string UserN { get => _userN; set { _userN = value; OnPropertyChanged(); RebuildComparisonIfNeeded(); } }
        public string UserQy { get => _userQy; set { _userQy = value; OnPropertyChanged(); RebuildComparisonIfNeeded(); } }
        public string UserQz { get => _userQz; set { _userQz = value; OnPropertyChanged(); RebuildComparisonIfNeeded(); } }
        public string UserMx { get => _userMx; set { _userMx = value; OnPropertyChanged(); RebuildComparisonIfNeeded(); } }
        public string UserMy { get => _userMy; set { _userMy = value; OnPropertyChanged(); RebuildComparisonIfNeeded(); } }
        public string UserMz { get => _userMz; set { _userMz = value; OnPropertyChanged(); RebuildComparisonIfNeeded(); } }
        public string UserMw { get => _userMw; set { _userMw = value; OnPropertyChanged(); RebuildComparisonIfNeeded(); } }

        public ConnectionOptionViewModel SelectedConnectionOption
        {
            get => _selectedConnectionOption;
            set
            {
                if (ReferenceEquals(_selectedConnectionOption, value))
                    return;

                _selectedConnectionOption = value;
                OnPropertyChanged();

                // Keep topology text in sync with selection, but don't touch preview
                var code = _selectedConnectionOption?.Code;
                if (!string.IsNullOrWhiteSpace(code))
                {
                    _nodeTopology = code;
                    OnPropertyChanged(nameof(NodeTopology));
                }
            }
        }

        public ImageSource SelectedNodeImage
        {
            get => _selectedNodeImage;
            private set { _selectedNodeImage = value; OnPropertyChanged(); }
        }

        /// <summary>Текст индикатора "1 / 3" для навигации по изображениям.</summary>
        public string ImageIndexText => _nodeImages.Count > 1
            ? $"{_nodeImageIndex + 1} / {_nodeImages.Count}"
            : string.Empty;

        /// <summary>Видимость стрелок навигации (только если больше 1 изображения).</summary>
        public bool HasMultipleImages => _nodeImages.Count > 1;

        public string SelectedNodeDescription
        {
            get => _selectedNodeDescription;
            private set { _selectedNodeDescription = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            private set { _status = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Элемент секции балки для отображения в превью и передачи в калькулятор 
        /// (может быть пустым, если не задано в interaction_tables.json).
        /// </summary>
        public string ElementSectionBeam
        {
            get => _elementSectionBeam;
            set
            {
                _elementSectionBeam = value;
                OnPropertyChanged();
                if (_isInitialized)
                {
                    RebuildConnectionCodeItemsFromInteractionTables();
                    UpdateStandardNodeFromJson();
                }
            }
        }

        /// <summary>
        /// Элемент секции колонны для отображения в превью и передачи в калькулятор 
        /// (может быть пустым, если не задано в interaction_tables.json).
        /// При выборе профиля колонны автоматически обновляется список доступных профилей балок
        /// и определяется код соединения.
        /// </summary>
        public string ElementSectionColumn
        {
            get => _elementSectionColumn;
            set
            {
                _elementSectionColumn = value;
                OnPropertyChanged();
                if (_isInitialized)
                {
                    RebuildBeamListByColumn();
                    RebuildConnectionCodeItemsFromInteractionTables();
                    UpdateStandardNodeFromJson();
                }
            }
        }

        public ObservableCollection<string> ConnectionCodeItems { get; }

        /// <summary>
        /// Фильтр элементов для расчёта. Например: "73-80" или "73 74 75 76 77 78 79 80".
        /// Если пусто — используются все элементы из загруженных данных.
        /// </summary>
        public string ElementFilterText
        {
            get => _elementFilterText;
            set { _elementFilterText = value; OnPropertyChanged(); }
        }

        /// <summary>Коэффициент надёжности ?_f — единый множитель для всех расчётных усилий. По умолчанию 1.</summary>
        public string GammaF
        {
            get => _gammaF;
            set
            {
                if (string.Equals(_gammaF, value, StringComparison.Ordinal)) return;
                _gammaF = value;
                OnPropertyChanged();
                SolutionSettings.SaveGammaF(value);
                if (_hasCalcData)
                {
                    ExecuteCalculation();
                }
            }
        }

        /// <summary>Перестраивает сравнительную диаграмму, если есть данные узла или расчёта.</summary>
        private void RebuildComparisonIfNeeded()
        {
            if (_hasCalcData || _lastStandardNodeData != null)
                BuildComparisonChart(_lastCalcNPlus, _lastCalcNMinus, _lastCalcQAbs, _lastCalcQzAbs, _lastCalcTAbs, _lastCalcMAbs, _lastCalcMoAbs, _lastCalcMwAbs);
        }

        /// <summary>
        /// Парсит строковое значение коэффициента (например, "1.05") в double.
        /// Поддерживает запятую и точку как десятичный разделитель. Возвращает 1 при ошибке.
        /// </summary>
        private static double ParseCoeff(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 1d;
            var t = s.Trim().Replace(',', '.');
            if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return d;
            return 1d;
        }

        public string ConnectionName
        {
            get => _connectionName;
            set
            {
                var v = (value ?? string.Empty).Trim();
                if (string.Equals(_connectionName, v, StringComparison.OrdinalIgnoreCase))
                    return;

                _connectionName = v;
                OnPropertyChanged();

                if (!_isInitialized)
                    return;

                // Preview follows Name only
                UpdateSelectedNodePresentation();

                // Загрузить доступные профили балок и колонн для выбранного Name
                RebuildProfileListFromInteractionTables();

                // Обновить список доступных CONNECTION_CODE для выбранного Name
                RebuildConnectionCodeItemsFromInteractionTables();

                UpdateStandardNodeFromJson();
            }
        }

        public string StandardConnectionCode
        {
            get => _standardConnectionCode;
            set
            {
                var v = (value ?? string.Empty).Trim();
                if (string.Equals(_standardConnectionCode, v, StringComparison.OrdinalIgnoreCase))
                    return;

                _standardConnectionCode = v;
                OnPropertyChanged();

                if (!_isInitialized)
                    return;

                UpdateStandardNodeFromJson();
            }
        }

        public ICommand ClearDataCommand { get; }
        public ICommand PasteRsuCommand { get; }
        public ICommand PasteRsnCommand { get; }
        public ICommand AddRsuCommand { get; }
        public ICommand AddRsnCommand { get; }
        public ICommand ExecuteCalculationCommand { get; }

        public ICommand ImportRsuFromExcelCommand { get; }
        public ICommand ImportRsnFromExcelCommand { get; }

        public ICommand PickElementSectionCommand { get; }
        public ICommand PickConnectionNameCommand { get; }
        public ICommand CopyIdeaToClipboardCommand { get; }
        public ICommand CopyAnalysisToClipboardCommand { get; }
        public ICommand CopyResultsToClipboardCommand { get; }
        public ICommand CopyNodeDataToClipboardCommand { get; }
        public RelayCommand PrevImageCommand { get; }
        public RelayCommand NextImageCommand { get; }
        public ICommand MinimizeCommand { get; }
        public ICommand MaximizeRestoreCommand { get; }
        public ICommand CloseCommand { get; }

        public ObservableCollection<string> ElementSectionsBeam { get; }
        public ObservableCollection<string> ElementSectionsColumn { get; }
        public ObservableCollection<string> ConnectionCodes { get; }

        /// <summary>Состояние окна (Normal / Maximized / Minimized) с отслеживанием OnPropertyChanged.</summary>
        public WindowState CurrentWindowState
        {
            get => _windowState;
            set
            {
                if (_windowState == value) return;
                _windowState = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MaximizeRestoreIcon));
            }
        }

        /// <summary>Иконка кнопки развернуть/восстановить: ? или ?.</summary>
        public string MaximizeRestoreIcon => _windowState == WindowState.Maximized ? "\uE923" : "\uE922";

        public StandardNodeInteractionViewModel StandardNode
        {
            get => _standardNode;
            private set { _standardNode = value; OnPropertyChanged(); }
        }
        private StandardNodeInteractionViewModel _standardNode;

        /// <summary>Пояснения из поля Explanations документа базы данных.</summary>
        public string NodeExplanation
        {
            get => _nodeExplanation;
            private set { _nodeExplanation = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNodeExplanation)); }
        }
        private string _nodeExplanation = string.Empty;

        public bool HasNodeExplanation => !string.IsNullOrWhiteSpace(_nodeExplanation);

        /// <summary>
        /// Обновляет превью узла: загружает все изображения для текущего имени соединения,
        /// устанавливает описание и сбрасывает навигацию по изображениям.
        /// </summary>
        private void UpdateSelectedNodePresentation()
        {
            var name = (ConnectionName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                SelectedNodeDescription = string.Empty;
                _nodeImages = new List<ImageSource>();
                _nodeImageIndex = 0;
                SelectedNodeImage = null;
                OnPropertyChanged(nameof(ImageIndexText));
                OnPropertyChanged(nameof(HasMultipleImages));
                PrevImageCommand?.RaiseCanExecuteChanged();
                NextImageCommand?.RaiseCanExecuteChanged();
                return;
            }

            SelectedNodeDescription = name;
            // Сбрасываем изображения сразу; загрузка из MongoDB — асинхронно
            _nodeImages = new List<ImageSource>();
            _nodeImageIndex = 0;
            SelectedNodeImage = null;
            OnPropertyChanged(nameof(ImageIndexText));
            OnPropertyChanged(nameof(HasMultipleImages));
            PrevImageCommand?.RaiseCanExecuteChanged();
            NextImageCommand?.RaiseCanExecuteChanged();
            _ = LoadNodeImagesAsync(name);
        }

        /// <summary>Переключает превью на предыдущее изображение узла (циклически).</summary>
        private void PrevImage()
        {
            if (_nodeImages.Count <= 1) return;
            _nodeImageIndex = (_nodeImageIndex - 1 + _nodeImages.Count) % _nodeImages.Count;
            SelectedNodeImage = _nodeImages[_nodeImageIndex];
            OnPropertyChanged(nameof(ImageIndexText));
        }

        /// <summary>Переключает превью на следующее изображение узла (циклически).</summary>
        private void NextImage()
        {
            if (_nodeImages.Count <= 1) return;
            _nodeImageIndex = (_nodeImageIndex + 1) % _nodeImages.Count;
            SelectedNodeImage = _nodeImages[_nodeImageIndex];
            OnPropertyChanged(nameof(ImageIndexText));
        }

        /// <summary>Асинхронно загружает изображения узла из MongoDB (fire-and-forget).</summary>
        private async Task LoadNodeImagesAsync(string name)
        {
            if (_nodeImageService == null) return;
            try
            {
                var images = await _nodeImageService.LoadAllNodeImagesAsync(name);
                _nodeImages = images;
                _nodeImageIndex = 0;
                SelectedNodeImage = _nodeImages.Count > 0 ? _nodeImages[0] : null;
                OnPropertyChanged(nameof(ImageIndexText));
                OnPropertyChanged(nameof(HasMultipleImages));
                PrevImageCommand?.RaiseCanExecuteChanged();
                NextImageCommand?.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadNodeImages error: {ex.Message}");
            }
        }


        /// <summary>
        /// Очищает все загруженные данные: РСУ, РСН, результаты расчёта,
        /// таблицу анализа, диаграмму сравнения, таблицу IDEA и файлы версий расчётов.
        /// </summary>
        private void ClearData()
        {
            RsuRows.Clear();
            RsnRows.Clear();
            Results.Clear();
            AnalysisRows.Clear();
            ComparisonItems.Clear();
            IdeaRows.Clear();
            _lastStandardNodeData = null;
            _hasCalcData = false;
            _lastCalcNPlus = null;
            _lastCalcNMinus = null;
            _lastCalcQAbs = null;
            _lastCalcQzAbs = null;
            _lastCalcTAbs = null;
            _lastCalcMAbs = null;
            _lastCalcMoAbs = null;
            _lastCalcMwAbs = null;

            // Удалить все файлы версий расчётов
            try
            {
                foreach (var f in GetResultFiles())
                    File.Delete(f);
            }
            catch
            {
                // ignore file deletion errors
            }
            CalculationVersions.Clear();
            _selectedCalculationVersion = null;
            OnPropertyChanged(nameof(SelectedCalculationVersion));

            Status = "Cleared";
        }

        /// <summary>Циклически переключает текущий профиль балки и колонны на следующий в списке.</summary>
        private void PickElementSection()
        {
            var listBeam = ElementSectionsBeam.ToArray();
            ElementSectionBeam = CycleInList(ElementSectionBeam, listBeam);

            var listColumn = ElementSectionsColumn.ToArray();
            ElementSectionColumn = CycleInList(ElementSectionColumn, listColumn);

            Status = "Section set: Beam=" + ElementSectionBeam + "; Column=" + ElementSectionColumn;
        }

        /// <summary>Циклически переключает имя группы узловых соединений на следующее в списке.</summary>
        private void PickConnectionName()
        {
            var list = (ConnectionCodes != null && ConnectionCodes.Count > 0)
                ? ConnectionCodes.ToArray()
                : Array.Empty<string>();

            ConnectionName = CycleInList(ConnectionName, list);
            Status = "Name set: " + ConnectionName;
        }

        /// <summary>
        /// Возвращает следующий элемент из массива после текущего (циклически).
        /// Если текущий не найден — возвращает первый элемент.
        /// </summary>
        private static string CycleInList(string current, string[] list)
        {
            if (list == null || list.Length == 0) return string.Empty;

            int idx = -1;
            if (!string.IsNullOrWhiteSpace(current))
                idx = Array.FindIndex(list, x => string.Equals(x, current, StringComparison.OrdinalIgnoreCase));

            idx = (idx + 1) % list.Length;
            return list[idx] ?? string.Empty;
        }

        /// <summary>
        /// Читает результат из JSON-файла и сохраняет его в MongoDB (коллекция Result).
        /// </summary>
        private void SaveResultToMongo(string jsonPath, double gammaF, int rsuCount, int rsnCount, HashSet<string> elemFilter)
        {
            try
            {
                if (!File.Exists(jsonPath)) return;

                var json = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);
                var parsed = Rs1ResultJsonSerializer.FromJson(json);

                var entity = new Domain.Entities.CalculationResult
                {
                    Id = Guid.NewGuid(),
                    CreatedAtUtc = DateTime.UtcNow,
                    GammaF = gammaF,
                    ElementFilter = elemFilter != null ? string.Join(",", elemFilter) : null,
                    SummaryN = parsed.SummaryN,
                    SummaryNt = parsed.SummaryNt,
                    SummaryNc = parsed.SummaryNc,
                    SummaryQy = parsed.SummaryQy,
                    SummaryQz = parsed.SummaryQz,
                    SummaryMx = parsed.SummaryMx,
                    SummaryMy = parsed.SummaryMy,
                    SummaryMz = parsed.SummaryMz,
                    SummaryMw = parsed.SummaryMw,
                    MaxU = parsed.MaxU,
                    SummaryPsi = parsed.SummaryPsi,
                    RsuCount = rsuCount,
                    RsnCount = rsnCount,
                };

                if (parsed.AnalysisRows != null)
                {
                    foreach (var r in parsed.AnalysisRows)
                    {
                        if (r == null) continue;
                        entity.AnalysisRows.Add(new Domain.Entities.CalculationResultAnalysisRow
                        {
                            RowType = r.RowType,
                            LoadCombination = r.LoadCombination,
                            Element = r.Element,
                            N = r.N,
                            Nt = r.Nt,
                            Nc = r.Nc,
                            Qy = r.Qy,
                            Qz = r.Qz,
                            Mx = r.Mx,
                            My = r.My,
                            Mz = r.Mz,
                            Mw = r.Mw,
                            U = r.U,
                            Psi = r.Psi,
                        });
                    }
                }

                // Сохраняем асинхронно в фоновом потоке, чтобы не блокировать UI
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _calculationResultRepository.AddAsync(entity);
                    }
                    catch (Exception mongoEx)
                    {
                        // Обновляем статус в UI-потоке
                        System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            Status += $" | Ошибка MongoDB: {mongoEx.Message}";
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                // Ошибка подготовки данных не должна блокировать основной расчёт
                Status += $" | Ошибка MongoDB: {ex.Message}";
            }
        }

        /// <summary>
        /// Загружает результат расчёта из JSON-файла (Result_vXXX.json) и заполняет коллекции UI.
        /// </summary>
        private void LoadResultFromJson(string path)
        {
            if (!File.Exists(path))
            {
                Status = Path.GetFileName(path) + " не найден";
                return;
            }

            var json = File.ReadAllText(path, System.Text.Encoding.UTF8);
            var result = Rs1ResultJsonSerializer.FromJson(json);

            // UI формирует таблицу из расчётных данных
            AnalysisRows.Clear();
            if (result.AnalysisRows != null)
            {
                foreach (var r in result.AnalysisRows)
                {
                    if (r == null) continue;
                    AnalysisRows.Add(new Rs1AnalysisRowViewModel
                    {
                        RowType = r.RowType,
                        LoadCombination = r.LoadCombination,
                        Element = r.Element,
                        N = r.N,
                        Nt = r.Nt,
                        Nc = r.Nc,
                        Qy = r.Qy,
                        Qz = r.Qz,
                        Mx = r.Mx,
                        My = r.My,
                        Mz = r.Mz,
                        Mw = r.Mw,
                        U = r.U,
                        Psi = r.Psi,
                    });
                }
            }

            // UI формирует сводку из расчётных данных
            Results.Clear();
            Results.Add(new ResultItemViewModel { Key = "N (кН)", Value = FormatOrDash(result.SummaryN) });
            Results.Add(new ResultItemViewModel { Key = "Nt (кН)", Value = FormatOrDash(result.SummaryNt) });
            Results.Add(new ResultItemViewModel { Key = "Nc (кН)", Value = FormatOrDash(result.SummaryNc) });
            Results.Add(new ResultItemViewModel { Key = "Qy (кН)", Value = FormatOrDash(result.SummaryQy) });
            Results.Add(new ResultItemViewModel { Key = "Qz (кН)", Value = FormatOrDash(result.SummaryQz) });
            Results.Add(new ResultItemViewModel { Key = "Mz (кН·м)", Value = FormatOrDash(result.SummaryMz) });
            Results.Add(new ResultItemViewModel { Key = "Mx (кН·м)", Value = FormatOrDash(result.SummaryMx) });
            Results.Add(new ResultItemViewModel { Key = "My (кН·м)", Value = FormatOrDash(result.SummaryMy) });
            Results.Add(new ResultItemViewModel { Key = "Mw (кН*м?)", Value = FormatOrDash(result.SummaryMw) });
            Results.Add(new ResultItemViewModel { Key = "u (макс.)", Value = FormatOrDash(result.MaxU) });
            Results.Add(new ResultItemViewModel { Key = "?", Value = FormatOrDash(result.SummaryPsi) });

            _lastCalcNPlus = result.SummaryNt;
            _lastCalcNMinus = result.SummaryNc;
            _lastCalcQAbs = result.SummaryQy;
            _lastCalcQzAbs = result.SummaryQz;
            _lastCalcMoAbs = result.SummaryMz;
            _lastCalcTAbs = result.SummaryMx;
            _lastCalcMAbs = result.SummaryMy;
            _lastCalcMwAbs = result.SummaryMw;
            _hasCalcData = true;

            BuildIdeaStaticaTable();
        }

        /// <summary>
        /// Определяет каталог для хранения файлов результатов расчёта (Result_vXXX.json).
        /// Результаты сохраняются в steel_structures_nodes.Calculate\ResultCalculate\.
        /// </summary>
        private static string GetResultDir()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Поиск корня репозитория > steel_structures_nodes.Calculate\ResultCalculate
            var dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 10 && dir != null; i++)
            {
                var candidate = Path.Combine(dir.FullName, "steel_structures_nodes.Calculate", "ResultCalculate");
                // Каталог проекта steel_structures_nodes.Calculate существует — используем его подкаталог
                var projectDir = Path.Combine(dir.FullName, "steel_structures_nodes.Calculate");
                if (Directory.Exists(projectDir))
                {
                    if (!Directory.Exists(candidate))
                        Directory.CreateDirectory(candidate);
                    return candidate;
                }
                dir = dir.Parent;
            }

            // Fallback: ResultCalculate рядом с exe
            var fallback = Path.Combine(baseDir, "ResultCalculate");
            if (!Directory.Exists(fallback))
                Directory.CreateDirectory(fallback);
            return fallback;
        }

        /// <summary>
        /// Возвращает все файлы Result_vXXX.json из каталога результатов, отсортированные по версии.
        /// </summary>
        private static List<string> GetResultFiles()
        {
            var resultDir = GetResultDir();
            if (!Directory.Exists(resultDir))
                return new List<string>();

            return Directory.GetFiles(resultDir, "Result_v*.json")
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Обновляет список доступных версий расчётов для UI.
        /// </summary>
        private void RebuildCalculationVersions()
        {
            CalculationVersions.Clear();
            foreach (var f in GetResultFiles())
                CalculationVersions.Add(Path.GetFileNameWithoutExtension(f));

            if (CalculationVersions.Count > 0)
                SelectedCalculationVersion = CalculationVersions[CalculationVersions.Count - 1];
        }

        /// <summary>Список доступных версий расчётов (имена файлов без расширения).</summary>
        public ObservableCollection<string> CalculationVersions { get; } = new();

        private string _selectedCalculationVersion;
        /// <summary>Выбранная версия расчёта для отображения.</summary>
        public string SelectedCalculationVersion
        {
            get => _selectedCalculationVersion;
            set
            {
                if (string.Equals(_selectedCalculationVersion, value, StringComparison.Ordinal))
                    return;
                _selectedCalculationVersion = value;
                OnPropertyChanged();

                // При смене версии — загрузить данные из соответствующего файла
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var path = Path.Combine(GetResultDir(), value + ".json");
                    if (File.Exists(path))
                    {
                        LoadResultFromJson(path);
                        if (_hasCalcData)
                            BuildComparisonChart(_lastCalcNPlus, _lastCalcNMinus, _lastCalcQAbs, _lastCalcQzAbs, _lastCalcTAbs, _lastCalcMAbs, _lastCalcMoAbs, _lastCalcMwAbs);
                        Status = $"Загружена версия: {value}";
                    }
                }
            }
        }

        /// <summary>
        /// Загружает данные стандартного узла из MongoDB по выбранному имени и коду соединения.
        /// Заполняет все свойства StandardNode: геометрию, несущую способность,
        /// болты, сварку, коэффициенты взаимодействия и жёсткость.
        /// </summary>
        private void UpdateStandardNodeFromJson()
        {
            var name = (ConnectionName ?? string.Empty).Trim();
            var code = (StandardConnectionCode ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code))
            {
                StandardNode.ClearAll();
                Status = "Standard node: empty selection";
                return;
            }

            StandardNodeData data;
            try
            {
                data = _interactionService.LoadStandardNode(name, code);
            }
            catch (Exception ex)
            {
                _lastStandardNodeData = null;
                Status = "Ошибка загрузки узла: " + ex.Message;
                return;
            }
            if (data == null)
            {
                _lastStandardNodeData = null;
                Status = "Standard node NOT FOUND: Name=" + name + "; CONNECTION_CODE=" + code;
                return;
            }

            _lastStandardNodeData = data;
            ApplyStandardNodeData(data);

            Status = "Standard node loaded";

            // Обновить диаграмму: табличные значения всегда, расчётные — если есть
            BuildComparisonChart(_lastCalcNPlus, _lastCalcNMinus, _lastCalcQAbs, _lastCalcQzAbs, _lastCalcTAbs, _lastCalcMAbs, _lastCalcMoAbs, _lastCalcMwAbs);
        }

        /// <summary>
        /// Асинхронная версия UpdateStandardNodeFromJson: выполняет MongoDB-запрос
        /// в фоновом потоке, а обновление UI — через Dispatcher.
        /// Используется из ExecuteCalculation, чтобы не блокировать отрисовку графика.
        /// </summary>
        private void UpdateStandardNodeFromJsonAsync(string calcStatus)
        {
            var name = (ConnectionName ?? string.Empty).Trim();
            var code = (StandardConnectionCode ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code))
            {
                Status = calcStatus;
                return;
            }

            _ = Task.Run(() =>
            {
                try
                {
                    return _interactionService.LoadStandardNode(name, code);
                }
                catch
                {
                    return null;
                }
            }).ContinueWith(task =>
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    if (task.IsCompletedSuccessfully && task.Result != null)
                    {
                        // Данные получены — обновляем UI через существующий синхронный метод,
                        // но данные уже загружены, поэтому повторный вызов быстрый (кэш MongoDB драйвера).
                        // Проще — напрямую применить данные:
                        _lastStandardNodeData = task.Result;
                        ApplyStandardNodeData(task.Result);

                        SyncProfilePickersToNodeData(task.Result);

                        // Обновить диаграмму: табличные значения всегда, расчётные — если есть
                        BuildComparisonChart(_lastCalcNPlus, _lastCalcNMinus, _lastCalcQAbs, _lastCalcQzAbs, _lastCalcTAbs, _lastCalcMAbs, _lastCalcMoAbs, _lastCalcMwAbs);
                    }

                    Status = calcStatus;
                });
            });
        }

        /// <summary>
        /// Применяет данные StandardNodeData к свойствам StandardNode (UI).
        /// Вынесено из UpdateStandardNodeFromJson для повторного использования.
        /// </summary>
        private void ApplyStandardNodeData(StandardNodeData data)
        {
            Func<double?, string> f = StandardNodeInteractionViewModel.F;
            StandardNode.ClearAll();
            //Тип узла
            StandardNode.TypeNode = data.TypeNode ?? string.Empty;
            //Марка
            StandardNode.TableBrand = data.TableBrand ?? string.Empty;
            // Пояснения из базы данных
            NodeExplanation = data.Explanations ?? string.Empty;
            #region Внутренние усилия
            StandardNode.Nt = f(data.Nt ?? data.N);
            StandardNode.Nc = f(data.Nc ?? (data.N.HasValue ? -Math.Abs(data.N.Value) : (double?)null));
            StandardNode.N = f(data.N);
            StandardNode.Qy = f(data.Qy);
            StandardNode.Qz = f(data.Qz);
            StandardNode.Mx = f(data.Mx ?? data.T);
            StandardNode.Mw = f(data.Mw);
            StandardNode.My = f(data.My);
            StandardNode.Mneg = f(data.Mneg);
            StandardNode.Mz = f(data.Mz); 
            #endregion
            #region Коэффициенты взаимодействия

            StandardNode.Alpha = f(data.Alpha);
            StandardNode.Beta = f(data.Beta);
            StandardNode.Gamma = f(data.Gamma);
            StandardNode.Delta = f(data.Delta);
            StandardNode.Epsilon = f(data.Epsilon);
            StandardNode.Lambda = f(data.Lambda);
            StandardNode.Variable = f(data.Variable);
            #endregion
            #region Жесткость 
            StandardNode.Sj = f(data.Sj);
            StandardNode.Sjo = f(data.Sjo);
            #endregion
            #region Балки
            StandardNode.ProfileBeam = data.ProfileBeam ?? string.Empty;
            StandardNode.BeamH = f(data.BeamH);
            StandardNode.BeamB = f(data.BeamB);
            StandardNode.BeamS = f(data.BeamS);
            StandardNode.BeamT = f(data.BeamT);
            StandardNode.BeamA = f(data.BeamA);
            StandardNode.BeamP = f(data.BeamP);
            StandardNode.BeamIz = f(data.BeamIz);
            StandardNode.BeamIy = f(data.BeamIy);
            StandardNode.BeamIx = f(data.BeamIx);
            StandardNode.BeamWz = f(data.BeamWz);
            StandardNode.BeamWy = f(data.BeamWy);
            StandardNode.BeamWx = f(data.BeamWx);
            StandardNode.BeamSz = f(data.BeamSz);
            StandardNode.BeamSy = f(data.BeamSy);
            StandardNode.Beamiz = f(data.Beamiz);
            StandardNode.Beamiy = f(data.Beamiy);
            StandardNode.BeamXo = f(data.BeamXo);
            #endregion
            #region Колонны
            StandardNode.ProfileColumn = data.ProfileColumn ?? string.Empty;
            StandardNode.ColumnH = f(data.ColumnH);
            StandardNode.ColumnB = f(data.ColumnB);
            StandardNode.ColumnS = f(data.ColumnS);
            StandardNode.ColumnT = f(data.ColumnT);
            StandardNode.ColumnA = f(data.ColumnA);
            StandardNode.ColumnP = f(data.ColumnP);
            StandardNode.ColumnIz = f(data.ColumnIz);
            StandardNode.ColumnIy = f(data.ColumnIy);
            StandardNode.ColumnIx = f(data.ColumnIx);
            StandardNode.ColumnWz = f(data.ColumnWz);
            StandardNode.ColumnWy = f(data.ColumnWy);
            StandardNode.ColumnWx = f(data.ColumnWx);
            StandardNode.ColumnSz = f(data.ColumnSz);
            StandardNode.ColumnSy = f(data.ColumnSy);
            StandardNode.Columniz = f(data.Columniz);
            StandardNode.Columniy = f(data.Columniy);
            StandardNode.ColumnXo = f(data.ColumnXo);
            StandardNode.ColumnYo = f(data.ColumnYo);
            #endregion
            #region Пластины
            StandardNode.PlateH = f(data.PlateH);
            StandardNode.PlateB = f(data.PlateB);
            StandardNode.PlateT = f(data.PlateT);
            #endregion
            #region Фланец
            StandardNode.FlangeH = f(data.FlangeH);
            StandardNode.FlangeB = f(data.FlangeB);
            StandardNode.FlangeT = f(data.FlangeT);
            StandardNode.FlangeLb = f(data.FlangeLb);
            #endregion
            #region Ребра

            StandardNode.StiffTr1 = f(data.StiffTr1);
            StandardNode.StiffTr2 = f(data.StiffTr2);
            StandardNode.StiffTbp = f(data.StiffTbp);
            StandardNode.StiffTg = f(data.StiffTg);
            StandardNode.StiffTf = f(data.StiffTf);
            StandardNode.StiffLh = f(data.StiffLh);
            StandardNode.StiffHh = f(data.StiffHh);
            StandardNode.StiffTwp = f(data.StiffTwp);
            #endregion
            #region Болты
            StandardNode.BoltDiameter = f(data.BoltDiameter);
            StandardNode.BoltCount = data.BoltCount?.ToString() ?? "0";
            StandardNode.BoltRows = data.BoltRows?.ToString() ?? "0";
            StandardNode.BoltVersion = data.BoltVersion?.ToString() ?? "0";
            StandardNode.BoltCoordY = StandardNodeInteractionViewModel.FormatBoltCoordY(data.BoltCoordY);
            StandardNode.BoltCoordX = StandardNodeInteractionViewModel.FormatBoltCoordX(data.BoltCoordX);
            StandardNode.BoltCoordZ = f(data.BoltCoordZ);

            var cy = data.BoltCoordY;
            StandardNode.BoltE1 = cy != null && cy.Length > 0 ? f(cy[0]) : string.Empty;
            StandardNode.BoltP1 = cy != null && cy.Length > 1 ? f(cy[1]) : string.Empty;
            StandardNode.BoltP2 = cy != null && cy.Length > 2 ? f(cy[2]) : string.Empty;
            StandardNode.BoltP3 = cy != null && cy.Length > 3 ? f(cy[3]) : string.Empty;
            StandardNode.BoltP4 = cy != null && cy.Length > 4 ? f(cy[4]) : string.Empty;
            StandardNode.BoltP5 = cy != null && cy.Length > 5 ? f(cy[5]) : string.Empty;
            StandardNode.BoltP6 = cy != null && cy.Length > 6 ? f(cy[6]) : string.Empty;
            StandardNode.BoltP7 = cy != null && cy.Length > 7 ? f(cy[7]) : string.Empty;
            StandardNode.BoltP8 = cy != null && cy.Length > 8 ? f(cy[8]) : string.Empty;
            StandardNode.BoltP9 = cy != null && cy.Length > 9 ? f(cy[9]) : string.Empty;
            StandardNode.BoltP10 = cy != null && cy.Length > 10 ? f(cy[10]) : string.Empty;

            var cx = data.BoltCoordX;
            StandardNode.BoltD1 = cx != null && cx.Length > 0 ? f(cx[0]) : string.Empty;
            StandardNode.BoltD2 = cx != null && cx.Length > 1 ? f(cx[1]) : string.Empty;

            #endregion
            #region Сварка
            StandardNode.WeldKf = StandardNodeInteractionViewModel.FormatArray(data.WeldKf);

            var wk = data.WeldKf;
            StandardNode.WeldKf1 = wk != null && wk.Length > 0 ? StandardNodeInteractionViewModel.F(wk[0]) : string.Empty;
            StandardNode.WeldKf2 = wk != null && wk.Length > 1 ? StandardNodeInteractionViewModel.F(wk[1]) : string.Empty;
            StandardNode.WeldKf3 = wk != null && wk.Length > 2 ? StandardNodeInteractionViewModel.F(wk[2]) : string.Empty;
            StandardNode.WeldKf4 = wk != null && wk.Length > 3 ? StandardNodeInteractionViewModel.F(wk[3]) : string.Empty;
            StandardNode.WeldKf5 = wk != null && wk.Length > 4 ? StandardNodeInteractionViewModel.F(wk[4]) : string.Empty;
            StandardNode.WeldKf6 = wk != null && wk.Length > 5 ? StandardNodeInteractionViewModel.F(wk[5]) : string.Empty;
            StandardNode.WeldKf7 = wk != null && wk.Length > 6 ? StandardNodeInteractionViewModel.F(wk[6]) : string.Empty;
            StandardNode.WeldKf8 = wk != null && wk.Length > 7 ? StandardNodeInteractionViewModel.F(wk[7]) : string.Empty;
            StandardNode.WeldKf9 = wk != null && wk.Length > 8 ? StandardNodeInteractionViewModel.F(wk[8]) : string.Empty;
            StandardNode.WeldKf10 = wk != null && wk.Length > 9 ? StandardNodeInteractionViewModel.F(wk[9]) : string.Empty; 
            #endregion

        }

        /// <summary>
        /// Sync column/beam pickers to match loaded node data.
        /// </summary>
        private void SyncProfilePickersToNodeData(StandardNodeData data)
        {
            var savedInit = _isInitialized;
            _isInitialized = false;

            var col = data.ProfileColumn;
            if (!string.IsNullOrWhiteSpace(col) && ElementSectionsColumn.Contains(col))
                ElementSectionColumn = col;

            var beam = data.ProfileBeam;
            if (!string.IsNullOrWhiteSpace(beam) && ElementSectionsBeam.Contains(beam))
                ElementSectionBeam = beam;

            _isInitialized = savedInit;
        }

        /// <summary>
        /// Парсинг текстового фильтра элементов. Поддерживает:
        /// - Одиночные номера: "73 74 75"
        /// - Диапазоны через дефис: "73-80"
        /// - Диапазоны через "...": "73...80"
        /// - Разделители: пробел, запятая, точка с запятой
        /// </summary>
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

                // Range: "73...80"
                var dots = tok.IndexOf("...", StringComparison.Ordinal);
                if (dots > 0
                    && int.TryParse(tok.Substring(0, dots), NumberStyles.Integer, CultureInfo.InvariantCulture, out var a1)
                    && int.TryParse(tok.Substring(dots + 3), NumberStyles.Integer, CultureInfo.InvariantCulture, out var b1))
                {
                    for (int i = Math.Min(a1, b1); i <= Math.Max(a1, b1); i++)
                        set.Add(i.ToString(CultureInfo.InvariantCulture));
                    continue;
                }

                // Range: "73-80"
                var dash = tok.IndexOf('-');
                if (dash > 0 && dash < tok.Length - 1
                    && int.TryParse(tok.Substring(0, dash), NumberStyles.Integer, CultureInfo.InvariantCulture, out var a2)
                    && int.TryParse(tok.Substring(dash + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var b2))
                {
                    for (int i = Math.Min(a2, b2); i <= Math.Max(a2, b2); i++)
                        set.Add(i.ToString(CultureInfo.InvariantCulture));
                    continue;
                }

                // Single number
                if (int.TryParse(tok, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                {
                    set.Add(n.ToString(CultureInfo.InvariantCulture));
                    continue;
                }

                set.Add(tok);
            }

            return set;
        }

        /// <summary>
        /// Масштабирует строки усилий: умножает все значения на коэффициент ?_f.
        /// Возвращает новый список — исходные строки не модифицируются.
        /// </summary>
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
                    N = ScaleValue(r.N, gf),
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

        /// <summary>
        /// Умножает строковое числовое значение на коэффициент и возвращает новую строку.
        /// </summary>
        private static string ScaleValue(string s, double k)
        {
            if (k == 1d || string.IsNullOrWhiteSpace(s)) return s;
            var t = s.Trim().Replace(',', '.');
            if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return (d * k).ToString("G", CultureInfo.InvariantCulture);
            return s;
        }

        private static double? ParseNullableValue(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var t = s.Trim().Replace(',', '.');
            if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return d;
            return null;
        }

        /// <summary>Форматирует числовое значение (3 знака) или возвращает тире «—», если значение отсутствует.</summary>
        private static string FormatOrDash(double? v)
        {
            if (!v.HasValue) return "—";
            return v.Value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Строит сравнительную диаграмму «табличные vs расчётные» для всех силовых параметров.
        /// Вычисляет ширину полос и процент использования для каждого параметра.
        /// </summary>
        private void BuildComparisonChart(double? nPlus, double? nMinus, double? qAbs, double? qzAbs, double? tAbs, double? mAbs, double? moAbs, double? mwAbs)
        {
            ComparisonItems.Clear();

            // Расчётные значения уже содержат ?_f (силы масштабируются ДО калькулятора
            // в ExecuteCalculation), поэтому здесь повторное умножение НЕ требуется.

            // Fallback: если AlbumNt отсутствует, но N есть > AlbumNt = N (аналогично для AlbumN)
            var tableNt = _lastStandardNodeData?.Nt ?? _lastStandardNodeData?.Nt;
            var tableNc = _lastStandardNodeData?.Nc ?? (_lastStandardNodeData?.Nc.HasValue == true ? -Math.Abs(_lastStandardNodeData.Nc.Value) : (double?)null);
            var tableN = _lastStandardNodeData?.N ?? (_lastStandardNodeData?.N.HasValue == true ? -Math.Abs(_lastStandardNodeData.N.Value) : (double?)null);
            var tableMx = _lastStandardNodeData?.Mx ?? _lastStandardNodeData?.T;

            var userNt = ParseNullableValue(UserNt) ?? ParseNullableValue(UserN);
            var userNc = ParseNullableValue(UserNc) ?? ParseNullableValue(UserN);
            var userQy = ParseNullableValue(UserQy);
            var userQz = ParseNullableValue(UserQz);
            var userMx = ParseNullableValue(UserMx);
            var userMy = ParseNullableValue(UserMy);
            var userMz = ParseNullableValue(UserMz);
            var userMw = ParseNullableValue(UserMw);

            var items = new List<ComparisonChartItem>();
            AddChartItem(items, "Nt (растяж.), кН", tableNt, nPlus);
            AddChartItem(items, "Nc (сжат.), кН", tableNc, nMinus.HasValue ? Math.Abs(nMinus.Value) : (double?)null);
            AddChartItem(items, "N (+/-), кН", tableN, nMinus.HasValue ? Math.Abs(nMinus.Value) : (double?)null);
            AddChartItem(items, "Qy, кН", _lastStandardNodeData?.Qy, qAbs);
            AddChartItem(items, "Qz, кН", _lastStandardNodeData?.Qz, qzAbs);
            AddChartItem(items, "Mx, кН·м", tableMx, tAbs);
            AddChartItem(items, "My, кН·м", _lastStandardNodeData?.My, mAbs);
            AddChartItem(items, "Mz, кН·м", _lastStandardNodeData?.Mz, moAbs);
            AddChartItem(items, "Mw, кН*м?", _lastStandardNodeData?.Mw, mwAbs);

            const double maxBarWidth = 300;
            double maxVal = 1;
            foreach (var it in items)
            {
                if (it.TableValue > maxVal) maxVal = it.TableValue;
                if (it.CalcValue > maxVal) maxVal = it.CalcValue;
                if (it.UserValue > maxVal) maxVal = it.UserValue;
            }

            foreach (var it in items)
            {
                it.TableBarWidth = it.TableValue / maxVal * maxBarWidth;
                it.CalcBarWidth = it.CalcValue / maxVal * maxBarWidth;
                it.UserBarWidth = it.UserValue / maxVal * maxBarWidth;
                ComparisonItems.Add(it);
            }
        }

        /// <summary>
        /// Создаёт элемент сравнительной диаграммы: вычисляет абсолютные значения,
        /// процент использования и флаг превышения предела.
        /// </summary>
        private static void AddChartItem(List<ComparisonChartItem> items, string label, double? tableVal, double? calcVal)
        {
            AddChartItem(items, label, tableVal, calcVal, null);
        }

        private static void AddChartItem(List<ComparisonChartItem> items, string label, double? tableVal, double? calcVal, double? userVal)
        {
            var tv = tableVal.HasValue ? Math.Abs(tableVal.Value) : 0;
            var cv = calcVal.HasValue ? Math.Abs(calcVal.Value) : 0;
            var uv = userVal.HasValue ? Math.Abs(userVal.Value) : 0;

            var maxCompared = Math.Max(cv, uv);

            string ratioText;
            bool isOver;
            if ((!calcVal.HasValue && !userVal.HasValue) || (tv == 0 && cv == 0 && uv == 0))
            {
                // Нет расчётных/пользовательских данных или все значения нулевые — сравнение невозможно
                ratioText = "—";
                isOver = false;
            }
            else if (tv == 0)
            {
                ratioText = "?";
                isOver = true;
            }
            else
            {
                var pct = maxCompared / tv * 100;
                ratioText = pct.ToString("0.#", CultureInfo.InvariantCulture) + "%";
                isOver = maxCompared > tv;
            }

            items.Add(new ComparisonChartItem
            {
                Label = label,
                TableValue = tv,
                CalcValue = cv,
                UserValue = uv,
                TableText = tv == 0 ? "—" : tv.ToString("0.###", CultureInfo.InvariantCulture),
                CalcText = cv == 0 ? "—" : cv.ToString("0.###", CultureInfo.InvariantCulture),
                UserText = uv == 0 ? "—" : uv.ToString("0.###", CultureInfo.InvariantCulture),
                RatioText = ratioText,
                IsOverLimit = isOver,
            });
        }

        /// <summary>
        /// Строит таблицу нагрузок в нотации IDEA StatiCA из таблицы анализа.
        /// Маппинг наименований: Альбом > Лира > IDEA StatiCA:
        ///   N > N;  Qo > QY > Vy;  Q > QZ > Vz;  T > MK > Mx;  M > MY > My;  Mo > MZ > Mz
        /// </summary>
        private void BuildIdeaStaticaTable()
        {
            IdeaRows.Clear();

            // Маппинг: RowType анализа > название строки IDEA / Альбом
            // MAX N  > MAX N;  MAX N+ > MAX Nt;  MAX N- > MAX Nc
            // MAX Qy > MAX Qo (Vy);  MAX Qz > MAX Q (Vz)
            // MAX Mx > MAX T (Mx);  MAX My > MAX M (My);  MAX Mz > MAX Mo (Mz)
            // MAX Mw > MAX Mw;  MAX Coeff > MAX Coeff;  MAX u > MAX u
            foreach (var a in AnalysisRows)
            {
                var ideaName = MapRowTypeToIdea(a.RowType);

                IdeaRows.Add(new IdeaStaticaRowViewModel
                {
                    RowType = ideaName,
                    N = a.N,
                    Vy = a.Qy,
                    Vz = a.Qz,
                    Mx = a.Mx,
                    My = a.My,
                    Mz = a.Mz,
                });
            }
        }

        /// <summary>
        /// Маппинг наименований строк анализа в нотацию IDEA StatiCA.
        /// Например: «MAX Qy» > «MAX Qo», «MAX Mx» > «MAX T».
        /// </summary>
        internal static string MapRowTypeToIdea(string rowType)
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

    }

    public sealed class StandardNodeInteractionViewModel : ViewModelBase
    {
        #region Об узле
        //Тип узла(Rigid Joint - жесткое соединение, Hinged Joint - шарнирное соединение)
        private string _typeNode;
        private string _tableBrand;
        private string _variable;
        public string TypeNode { get => _typeNode; set { _typeNode = value; OnPropertyChanged(); } }
        public string TableBrand { get => _tableBrand; set { _tableBrand = value; OnPropertyChanged(); } }
        public string Variable { get => _variable; set { _variable = value; OnPropertyChanged(); } }

        #endregion
        #region Внутренние усилия
        //Внутренние усилия
        private string _nt;
        private string _nc;
        private string _n;
        private string _qy;
        private string _qz;
        private string _mx;
        private string _mw;
        private string _my;
        private string _mneg;
        private string _mz;
        public string Nt { get => _nt; set { _nt = value; OnPropertyChanged(); } }
        public string Nc { get => _nc; set { _nc = value; OnPropertyChanged(); } }
        public string N { get => _n; set { _n = value; OnPropertyChanged(); } }
        public string Qy { get => _qy; set { _qy = value; OnPropertyChanged(); } }
        public string Qz { get => _qz; set { _qz = value; OnPropertyChanged(); } }
        public string Mx { get => _mx; set { _mx = value; OnPropertyChanged(); } }
        public string Mw { get => _mw; set { _mw = value; OnPropertyChanged(); } }
        public string My { get => _my; set { _my = value; OnPropertyChanged(); } }
        public string Mneg { get => _mneg; set { _mneg = value; OnPropertyChanged(); } }
        public string Mz { get => _mz; set { _mz = value; OnPropertyChanged(); } } 
        #endregion
        #region Жёсткость
        //Жёсткость
        private string _sj;
        private string _sjo;
        public string Sj { get => _sj; set { _sj = value; OnPropertyChanged(); } }
        public string Sjo { get => _sjo; set { _sjo = value; OnPropertyChanged(); } } 
        #endregion
        #region Коэффициенты взаимодействия
        //Коэффициенты взаимодействия
        private string _alpha;
        private string _beta;
        private string _gamma;
        private string _delta;
        private string _lambda;
        private string _epsilon;
        public string Alpha { get => _alpha; set { _alpha = value; OnPropertyChanged(); } }
        public string Beta { get => _beta; set { _beta = value; OnPropertyChanged(); } }
        public string Gamma { get => _gamma; set { _gamma = value; OnPropertyChanged(); } }
        public string Delta { get => _delta; set { _delta = value; OnPropertyChanged(); } }
        public string Epsilon { get => _epsilon; set { _epsilon = value; OnPropertyChanged(); } }
        public string Lambda { get => _lambda; set { _lambda = value; OnPropertyChanged(); } }
        #endregion
        #region Профиль балки
        private string _profileBeam;
        private string _beamH;
        private string _beamB;
        private string _beamS;
        private string _beamT;
        private string _beamA;
        private string _beamP;
        private string _beamIz;
        private string _beamIy;
        private string _beamIx;
        private string _beamWz;
        private string _beamWy;
        private string _beamWx;
        private string _beamSz;
        private string _beamSy;
        private string _beamiz;
        private string _beamiy;
        private string _beamXo;

        /// <summary>Имя профиля балки (например, <c>30Б2</c>).</summary>
        public string ProfileBeam { get => _profileBeam; set { _profileBeam = value; OnPropertyChanged(); } }
        public string BeamH { get => _beamH; set { _beamH = value; OnPropertyChanged(); } }
        public string BeamB { get => _beamB; set { _beamB = value; OnPropertyChanged(); } }
        public string BeamS { get => _beamS; set { _beamS = value; OnPropertyChanged(); } }
        public string BeamT { get => _beamT; set { _beamT = value; OnPropertyChanged(); } }
        public string BeamA { get => _beamA; set { _beamA = value; OnPropertyChanged(); } }
        public string BeamP { get => _beamP; set { _beamP = value; OnPropertyChanged(); } }
        public string BeamIz { get => _beamIz; set { _beamIz = value; OnPropertyChanged(); } }
        public string BeamIy { get => _beamIy; set { _beamIy = value; OnPropertyChanged(); } }
        public string BeamIx { get => _beamIx; set { _beamIx = value; OnPropertyChanged(); } }
        public string BeamWz { get => _beamWz; set { _beamWz = value; OnPropertyChanged(); } }
        public string BeamWy { get => _beamWy; set { _beamWy = value; OnPropertyChanged(); } }
        public string BeamWx { get => _beamWx; set { _beamWx = value; OnPropertyChanged(); } }
        public string BeamSz { get => _beamSz; set { _beamSz = value; OnPropertyChanged(); } }
        public string BeamSy { get => _beamSy; set { _beamSy = value; OnPropertyChanged(); } }
        public string Beamiz { get => _beamiz; set { _beamiz = value; OnPropertyChanged(); } }
        public string Beamiy { get => _beamiy; set { _beamiy = value; OnPropertyChanged(); } }
        public string BeamXo { get => _beamXo; set { _beamXo = value; OnPropertyChanged(); } } 
        #endregion
        #region Профиль колонны
        //Профиль колонны
        private string _profileColumn;
        private string _columnH;
        private string _columnB;
        private string _columnS;
        private string _columnT;
        private string _columnA;
        private string _columnP;
        private string _columnIz;
        private string _columnIy;
        private string _columnIx;
        private string _columnWz;
        private string _columnWy;
        private string _columnWx;
        private string _columnSz;
        private string _columnSy;
        private string _columniz;
        private string _columniy;
        private string _columnXo;
        private string _columnYo;
        /// <summary>Имя профиля колонны (например, <c>30К1</c>).</summary>
        public string ProfileColumn { get => _profileColumn; set { _profileColumn = value; OnPropertyChanged(); } }
        public string ColumnH { get => _columnH; set { _columnH = value; OnPropertyChanged(); } }
        public string ColumnB { get => _columnB; set { _columnB = value; OnPropertyChanged(); } }
        public string ColumnS { get => _columnS; set { _columnS = value; OnPropertyChanged(); } }
        public string ColumnT { get => _columnT; set { _columnT = value; OnPropertyChanged(); } }
        public string ColumnA { get => _columnA; set { _columnA = value; OnPropertyChanged(); } }
        public string ColumnP { get => _columnP; set { _columnP = value; OnPropertyChanged(); } }
        public string ColumnIz { get => _columnIz; set { _columnIz = value; OnPropertyChanged(); } }
        public string ColumnIy { get => _columnIy; set { _columnIy = value; OnPropertyChanged(); } }
        public string ColumnIx { get => _columnIx; set { _columnIx = value; OnPropertyChanged(); } }
        public string ColumnWz { get => _columnWz; set { _columnWz = value; OnPropertyChanged(); } }
        public string ColumnWy { get => _columnWy; set { _columnWy = value; OnPropertyChanged(); } }
        public string ColumnWx { get => _columnWx; set { _columnWx = value; OnPropertyChanged(); } }
        public string ColumnSz { get => _columnSz; set { _columnSz = value; OnPropertyChanged(); } }
        public string ColumnSy { get => _columnSy; set { _columnSy = value; OnPropertyChanged(); } }
        public string Columniz { get => _columniz; set { _columniz = value; OnPropertyChanged(); } }
        public string Columniy { get => _columniy; set { _columniy = value; OnPropertyChanged(); } }
        public string ColumnXo { get => _columnXo; set { _columnXo = value; OnPropertyChanged(); } }
        public string ColumnYo { get => _columnYo; set { _columnYo = value; OnPropertyChanged(); } }

        #endregion
        #region Пластина
        //Пластина
        private string _plateH;
        private string _plateB;
        private string _plateT;
        public string PlateH { get => _plateH; set { _plateH = value; OnPropertyChanged(); } }
        public string PlateB { get => _plateB; set { _plateB = value; OnPropertyChanged(); } }
        public string PlateT { get => _plateT; set { _plateT = value; OnPropertyChanged(); } }

        #endregion
        #region Фланец
        //Фланец
        private string _flangeH;
        private string _flangeB;
        private string _flangeT;
        private string _flangeLb;
        public string FlangeH { get => _flangeH; set { _flangeH = value; OnPropertyChanged(); } }
        public string FlangeB { get => _flangeB; set { _flangeB = value; OnPropertyChanged(); } }
        public string FlangeT { get => _flangeT; set { _flangeT = value; OnPropertyChanged(); } }
        public string FlangeLb { get => _flangeLb; set { _flangeLb = value; OnPropertyChanged(); } }

        #endregion
        #region Ребра жёсткости
        //Ребра жёсткости
        private string _stiffTr1;
        private string _stiffTr2;
        private string _stiffTbp;
        private string _stiffTg;
        private string _stiffTf;
        private string _stiffLh;
        private string _stiffHh;
        private string _stiffTwp;
        public string StiffTr1 { get => _stiffTr1; set { _stiffTr1 = value; OnPropertyChanged(); } }
        public string StiffTr2 { get => _stiffTr2; set { _stiffTr2 = value; OnPropertyChanged(); } }
        public string StiffTbp { get => _stiffTbp; set { _stiffTbp = value; OnPropertyChanged(); } }
        public string StiffTg { get => _stiffTg; set { _stiffTg = value; OnPropertyChanged(); } }
        public string StiffTf { get => _stiffTf; set { _stiffTf = value; OnPropertyChanged(); } }
        public string StiffLh { get => _stiffLh; set { _stiffLh = value; OnPropertyChanged(); } }
        public string StiffHh { get => _stiffHh; set { _stiffHh = value; OnPropertyChanged(); } }
        public string StiffTwp { get => _stiffTwp; set { _stiffTwp = value; OnPropertyChanged(); } }

        #endregion
        #region Болты
        //Версия исполнения болтов
        private string _boltVersion;
        //Межболтовые расстояния в координате Y (e1, p1…)
        private string _boltCoordZ;
        private string _boltDiameter;
        private string _boltCount;
        private string _boltRows;
        private string _boltCoordY;
        private string _boltCoordX;
        private string _boltE1;
        private string _boltP1;
        private string _boltP2;
        private string _boltP3;
        private string _boltP4;
        private string _boltP5;
        private string _boltP6;
        private string _boltP7;
        private string _boltP8;
        private string _boltP9;
        private string _boltP10;
        private string _boltD1;
        private string _boltD2;
        public string BoltVersion { get => _boltVersion; set { _boltVersion = value; OnPropertyChanged(); } }
        public string BoltCoordZ { get => _boltCoordZ; set { _boltCoordZ = value; OnPropertyChanged(); } }
        public string BoltDiameter { get => _boltDiameter; set { _boltDiameter = value; OnPropertyChanged(); } }
        public string BoltCount { get => _boltCount; set { _boltCount = value; OnPropertyChanged(); } }
        public string BoltRows { get => _boltRows; set { _boltRows = value; OnPropertyChanged(); } }
        public string BoltCoordY { get => _boltCoordY; set { _boltCoordY = value; OnPropertyChanged(); } }
        public string BoltCoordX { get => _boltCoordX; set { _boltCoordX = value; OnPropertyChanged(); } }

        public string BoltE1 { get => _boltE1; set { _boltE1 = value; OnPropertyChanged(); } }
        public string BoltP1 { get => _boltP1; set { _boltP1 = value; OnPropertyChanged(); } }
        public string BoltP2 { get => _boltP2; set { _boltP2 = value; OnPropertyChanged(); } }
        public string BoltP3 { get => _boltP3; set { _boltP3 = value; OnPropertyChanged(); } }
        public string BoltP4 { get => _boltP4; set { _boltP4 = value; OnPropertyChanged(); } }
        public string BoltP5 { get => _boltP5; set { _boltP5 = value; OnPropertyChanged(); } }
        public string BoltP6 { get => _boltP6; set { _boltP6 = value; OnPropertyChanged(); } }
        public string BoltP7 { get => _boltP7; set { _boltP7 = value; OnPropertyChanged(); } }
        public string BoltP8 { get => _boltP8; set { _boltP8 = value; OnPropertyChanged(); } }
        public string BoltP9 { get => _boltP9; set { _boltP9 = value; OnPropertyChanged(); } }
        public string BoltP10 { get => _boltP10; set { _boltP10 = value; OnPropertyChanged(); } }
        public string BoltD1 { get => _boltD1; set { _boltD1 = value; OnPropertyChanged(); } }
        public string BoltD2 { get => _boltD2; set { _boltD2 = value; OnPropertyChanged(); } } 
        #endregion
        #region Сварка
        private string _weldKf;
        private string _weldKf1;
        private string _weldKf2;
        private string _weldKf3;
        private string _weldKf4;
        private string _weldKf5;
        private string _weldKf6;
        private string _weldKf7;
        private string _weldKf8;
        private string _weldKf9;
        private string _weldKf10;
        public string WeldKf { get => _weldKf; set { _weldKf = value; OnPropertyChanged(); } }
        public string WeldKf1 { get => _weldKf1; set { _weldKf1 = value; OnPropertyChanged(); } }
        public string WeldKf2 { get => _weldKf2; set { _weldKf2 = value; OnPropertyChanged(); } }
        public string WeldKf3 { get => _weldKf3; set { _weldKf3 = value; OnPropertyChanged(); } }
        public string WeldKf4 { get => _weldKf4; set { _weldKf4 = value; OnPropertyChanged(); } }
        public string WeldKf5 { get => _weldKf5; set { _weldKf5 = value; OnPropertyChanged(); } }
        public string WeldKf6 { get => _weldKf6; set { _weldKf6 = value; OnPropertyChanged(); } }
        public string WeldKf7 { get => _weldKf7; set { _weldKf7 = value; OnPropertyChanged(); } }
        public string WeldKf8 { get => _weldKf8; set { _weldKf8 = value; OnPropertyChanged(); } }
        public string WeldKf9 { get => _weldKf9; set { _weldKf9 = value; OnPropertyChanged(); } }
        public string WeldKf10 { get => _weldKf10; set { _weldKf10 = value; OnPropertyChanged(); } }

        #endregion
        /// <summary>Сбрасывает все свойства узла в пустые строки (начальное состояние).</summary>
        public void ClearAll()
        {
            ProfileBeam = string.Empty;
            ProfileColumn = string.Empty;
            Nt = string.Empty; Nc = string.Empty; N = string.Empty;
            Qy = string.Empty; Qz = string.Empty;
            Mx = string.Empty; Mw = string.Empty; My = string.Empty; Mz = string.Empty;
            Mneg = string.Empty;
            Alpha = string.Empty; Beta = string.Empty; Gamma = string.Empty;
            Delta = string.Empty; Epsilon = string.Empty; Lambda = string.Empty;
            Variable = string.Empty; Sj = string.Empty; Sjo = string.Empty;
            BeamH = string.Empty; BeamB = string.Empty;
            BeamS = string.Empty; BeamT = string.Empty;

            ColumnH = string.Empty; ColumnB = string.Empty;
            ColumnS = string.Empty; ColumnT = string.Empty;

            PlateH = string.Empty; PlateB = string.Empty; PlateT = string.Empty;
            FlangeH = string.Empty; FlangeB = string.Empty;
            FlangeT = string.Empty; FlangeLb = string.Empty;
            StiffTr1 = string.Empty; StiffTr2 = string.Empty;
            StiffTbp = string.Empty; StiffTg = string.Empty;
            StiffTf = string.Empty; StiffLh = string.Empty;
            StiffHh = string.Empty; StiffTwp = string.Empty;

            BeamA = string.Empty; BeamP = string.Empty;
            BeamIz = string.Empty; BeamIy = string.Empty; BeamIx = string.Empty;
            BeamWz = string.Empty; BeamWy = string.Empty; BeamWx = string.Empty;
            BeamSz = string.Empty; BeamSy = string.Empty;
            Beamiz = string.Empty; Beamiy = string.Empty; BeamXo = string.Empty;

            ColumnA = string.Empty; ColumnP = string.Empty;
            ColumnIz = string.Empty; ColumnIy = string.Empty; ColumnIx = string.Empty;
            ColumnWz = string.Empty; ColumnWy = string.Empty; ColumnWx = string.Empty;
            ColumnSz = string.Empty; ColumnSy = string.Empty;
            Columniz = string.Empty; Columniy = string.Empty;
            ColumnXo = string.Empty; ColumnYo = string.Empty;

            BoltVersion = string.Empty; BoltCoordZ = string.Empty;
            TableBrand = string.Empty;

            BoltDiameter = string.Empty; BoltCount = string.Empty;
            BoltRows = string.Empty;
            BoltCoordY = string.Empty; BoltCoordX = string.Empty;
            BoltE1 = string.Empty;
            BoltP1 = string.Empty; BoltP2 = string.Empty; BoltP3 = string.Empty;
            BoltP4 = string.Empty; BoltP5 = string.Empty; BoltP6 = string.Empty;
            BoltP7 = string.Empty; BoltP8 = string.Empty; BoltP9 = string.Empty;
            BoltP10 = string.Empty;
            BoltD1 = string.Empty; BoltD2 = string.Empty;

            WeldKf = string.Empty;
            WeldKf1 = string.Empty; WeldKf2 = string.Empty; WeldKf3 = string.Empty;
            WeldKf4 = string.Empty; WeldKf5 = string.Empty; WeldKf6 = string.Empty;
            WeldKf7 = string.Empty; WeldKf8 = string.Empty; WeldKf9 = string.Empty;
            WeldKf10 = string.Empty;
        }

        /// <summary>Форматирует nullable double в строку (3 знака) или "0", если значение отсутствует.</summary>
        public static string F(double? v)
        {
            if (!v.HasValue) return "0";
            return v.Value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        public static string F(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return string.Empty;
            var t = v.Trim();
            var normalized = t.Replace(',', '.');
            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return d.ToString("0.###", CultureInfo.InvariantCulture);
            return t;
        }

        /// <summary>Форматирует массив double в строку с разделителем «; », пропуская нулевые значения.</summary>
        public static string FormatArray(double[] arr)
        {
            if (arr == null || arr.Length == 0) return string.Empty;
            var nonZero = arr.Where(v => v != 0).ToArray();
            if (nonZero.Length == 0) return string.Empty;
            return string.Join("; ", nonZero.Select(v => v.ToString("0.###", CultureInfo.InvariantCulture)));
        }

        public static string FormatArray(string[] arr)
        {
            if (arr == null || arr.Length == 0) return string.Empty;
            var values = arr
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => F(v))
                .Where(v => !string.IsNullOrWhiteSpace(v) && v != "0")
                .ToArray();
            if (values.Length == 0) return string.Empty;
            return string.Join("; ", values);
        }

        /// <summary>
        /// Форматирует координаты болтов по Y с именами: e1, p1, p2, …, p10.
        /// </summary>
        public static string FormatBoltCoordY(double[] arr)
        {
            if (arr == null || arr.Length == 0) return string.Empty;
            var labels = new[] { "e1", "p1", "p2", "p3", "p4", "p5", "p6", "p7", "p8", "p9", "p10" };
            var parts = new List<string>();
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == 0) continue;
                var label = i < labels.Length ? labels[i] : $"p{i}";
                parts.Add($"{label} = {arr[i].ToString("0.###", CultureInfo.InvariantCulture)}");
            }
            return parts.Count == 0 ? string.Empty : string.Join(";  ", parts);
        }

        /// <summary>
        /// Форматирует координаты болтов по X с именами: d1, d2, …
        /// </summary>
        public static string FormatBoltCoordX(double[] arr)
        {
            if (arr == null || arr.Length == 0) return string.Empty;
            var parts = new List<string>();
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == 0) continue;
                parts.Add($"d{i + 1} = {arr[i].ToString("0.###", CultureInfo.InvariantCulture)}");
            }
            return parts.Count == 0 ? string.Empty : string.Join(";  ", parts);
        }
    }
}
