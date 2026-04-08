using Steel_structures_nodes_public_project.Wpf.Mvvm;

namespace Steel_structures_nodes_public_project.Wpf.ViewModels
{
    /// <summary>
    /// Элемент сравнительной диаграммы: табличное значение vs расчётное.
    /// </summary>
    public sealed class ComparisonChartItem : ViewModelBase
    {
        /// <summary>
        /// Название сравниваемого параметра (например, «Макс. момент»).
        /// </summary>
        private string _label;
        /// <summary>
        /// Табличное значение параметра.
        /// </summary>
        private double _tableValue;
        /// <summary>
        /// Расчетные значения
        /// </summary>
        private double _calcValue;
        /// <summary>
        /// Текстовые поля для отображения табличного и расчётного значения (например, «100 Н·м» и «85 Н·м»).
        /// </summary>
        private string _tableText;
        /// <summary>
        /// Расчётное значение в виде текста для отображения (например, «85 Н·м»).
        /// </summary>
        private string _calcText;
        /// <summary>
        /// больше: табличное или расчётное. Это может влиять на цветовую индикацию (например, красный цвет для превышения табличного значения).
        /// </summary>
        private double _tableBarWidth;
        private double _calcBarWidth;
        private string _ratioText;
        private bool _isOverLimit;

        public string Label { get => _label; set { _label = value; OnPropertyChanged(); } }
        public double TableValue { get => _tableValue; set { _tableValue = value; OnPropertyChanged(); } }
        public double CalcValue { get => _calcValue; set { _calcValue = value; OnPropertyChanged(); } }
        public string TableText { get => _tableText; set { _tableText = value; OnPropertyChanged(); } }
        public string CalcText { get => _calcText; set { _calcText = value; OnPropertyChanged(); } }
        public double TableBarWidth { get => _tableBarWidth; set { _tableBarWidth = value; OnPropertyChanged(); } }
        public double CalcBarWidth { get => _calcBarWidth; set { _calcBarWidth = value; OnPropertyChanged(); } }
        /// <summary>Текст соотношения расчётного к табличному (например, «85%» или «—»).</summary>
        public string RatioText { get => _ratioText; set { _ratioText = value; OnPropertyChanged(); } }
        /// <summary>true, если расчётное значение превышает табличное.</summary>
        public bool IsOverLimit { get => _isOverLimit; set { _isOverLimit = value; OnPropertyChanged(); } }
    }
}
