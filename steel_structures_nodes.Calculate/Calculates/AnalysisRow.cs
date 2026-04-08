using System;

namespace Steel_structures_nodes_public_project.Calculate
{
    /// <summary>
    /// Строка таблицы анализа РС1: содержит усилия, коэффициенты U и ψ.
    /// </summary>
    public sealed class AnalysisRow
    {
        public string RowType { get; set; } // e.g. "MAX N+", "MAX Qz", "MAX Mz"
        public string LoadCombination { get; set; }    // Комбинации нагрузок (DCL set / sequence text)
        public int? Element { get; set; }
        public double? N { get; set; }    // Продольная сила N
        public double? Nt { get; set; }   // Растягивающая сила AlbumNt (N > 0)
        public double? Nc { get; set; }   // Сжимающая сила AlbumN (N < 0)
        public double? Qz { get; set; }   // Поперечная сила Qz (= QZ из SCAD)
        public double? Qy { get; set; }   // Поперечная сила Qy (= QY из SCAD)
        public double? Mx { get; set; }   // Крутящий момент Mx (= MX из SCAD)
        public double? My { get; set; }   // Изгибающий момент My (= MY из SCAD)
        public double? Mz { get; set; }   // Изгибающий момент Mz (= MZ из SCAD)
        public double? Mw { get; set; }   // Бимомент Mw (= MW из SCAD)
        public double? U { get; set; }
        public double? Psi { get; set; }
    }
}
