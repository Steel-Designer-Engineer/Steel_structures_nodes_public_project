using System.Globalization;
using steel_structures_nodes.Wpf.Mvvm;

namespace steel_structures_nodes.Wpf.ViewModels
{
    /// <summary>
    /// ViewModel строки таблицы IDEA StatiCA — усилия в нотации IDEA StatiCA.
    /// </summary>
    public sealed class IdeaStaticaRowViewModel : ViewModelBase
    {
        /// <summary>Критерий (MAX Coeff, MAX Vy, MAX Vz, …).</summary>
        public string RowType { get; set; }

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

        public string ElementText => string.Empty;
    }
}
