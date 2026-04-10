namespace steel_structures_nodes.Wpf.Models
{
    /// <summary>
    /// Данные стандартного узла: несущая способность, коэффициенты взаимодействия и параметры сечения.
    /// </summary>
    public class StandardNodeData
    {
        /// <summary>Тип узла(Rigid Joint - жесткое соединение, Hinged Joint - шарнирное соединение)</summary>
        public string TypeNode { get; set; }
        /// <summary>
        /// Шаг1.1: Добавление имени профиля балки (например, <c>30Б2</c>).
        /// </summary>
        public string ProfileBeam { get; set; }

        /// <summary>
        /// Шаг1.2: Добавление имени профиля колонны (например, <c>30К1</c>).
        /// </summary>
        public string ProfileColumn { get; set; }
        /// <summary>
        /// Значение < Nt > - растяжение в узле
        /// </summary>
        /// <remarks> Растяжение </remarks>
        public double? Nt { get; set; }
        /// <summary>
        /// Значение < -Nс > - сжатие в узле
        /// </summary>
        /// <remarks> Растяжение </remarks>
        public double? Nc { get; set; }
        /// <summary>
        /// Общее значение продольной силы, может быть и сжатием и растяжением < +/- N > - в узле
        /// </summary>
        /// <remarks> Растяжение/Сжатие </remarks>
        public double? N { get; set; }
        /// <summary>
        /// Поперечная сила в узле, действующая в направлении оси y < Qz > - в узле
        /// </summary>
        public double? Qy { get; set; }
        /// <summary>
        /// Поперечная сила в узле, действующая в направлении оси z < Qz > - в узле
        /// </summary>
        public double? Qz { get; set; }
        /// <summary>
        /// Изгибающий момент в плоскости наибольшей жесткости (обычно в плоскости y) < My > - в узле
        /// </summary>
        public double? My { get; set; }
        /// <summary>
        /// Изгибающий момент в плоскости наименьшей жесткости (обычно в плоскости z) < Mz > - в узле
        /// </summary>
        public double? Mz { get; set; }
        /// <summary>
        /// Крутящий момент в узле < Mx > - в узле
        /// </summary>
        public double? Mx { get; set; }
        /// <summary>
        /// Крутящий момент в узле < Mx > - в узле
        /// </summary>
        public double? Mw { get; set; }
        /// <summary>
        /// Крутящий момент в узле < T > - в узле
        /// </summary>
        public double? T { get; set; }
        public double? Mneg { get; set; }
        public double? Alpha { get; set; }
        public double? Beta { get; set; }
        public double? Gamma { get; set; }
        public double? Delta { get; set; }
        public double? Epsilon { get; set; }
        public double? Lambda { get; set; }
        public double? Variable { get; set; }
        public double? Sj { get; set; }
        public double? Sjo { get; set; }
        public double? BeamH { get; set; }
        public double? BeamB { get; set; }
        public double? BeamS { get; set; }
        public double? BeamT { get; set; }

        // Геометрия колонны
        public double? ColumnH { get; set; }
        public double? ColumnB { get; set; }
        public double? ColumnS { get; set; }
        public double? ColumnT { get; set; }

        // Пластина
        public double? PlateH { get; set; }
        public double? PlateB { get; set; }
        public double? PlateT { get; set; }

        // Фланец
        public double? FlangeLb { get; set; }
        public double? FlangeH { get; set; }
        public double? FlangeB { get; set; }
        public double? FlangeT { get; set; }

        // Рёбра жёсткости
        public double? StiffTr1 { get; set; }
        public double? StiffTr2 { get; set; }

        // Характеристики сечения балки
        public double? BeamA { get; set; }
        public double? BeamP { get; set; }
        public double? BeamIz { get; set; }
        public double? BeamIy { get; set; }
        public double? BeamIx { get; set; }
        public double? BeamWz { get; set; }
        public double? BeamWy { get; set; }
        public double? BeamWx { get; set; }
        public double? BeamSz { get; set; }
        public double? BeamSy { get; set; }
        public double? Beamiz { get; set; }
        public double? Beamiy { get; set; }
        public double? BeamXo { get; set; }

        // Характеристики сечения колонны
        public double? ColumnA { get; set; }
        public double? ColumnP { get; set; }
        public double? ColumnIz { get; set; }
        public double? ColumnIy { get; set; }
        public double? ColumnIx { get; set; }
        public double? ColumnWz { get; set; }
        public double? ColumnWy { get; set; }
        public double? ColumnWx { get; set; }
        public double? ColumnSz { get; set; }
        public double? ColumnSy { get; set; }
        public double? Columniz { get; set; }
        public double? Columniy { get; set; }
        public double? ColumnXo { get; set; }
        public double? ColumnYo { get; set; }

        // Рёбра жёсткости (дополнительные)
        public double? StiffTbp { get; set; }
        public double? StiffTg { get; set; }
        public double? StiffTf { get; set; }
        public double? StiffLh { get; set; }
        public double? StiffHh { get; set; }
        public double? StiffTwp { get; set; }

        // Болты
        public double? BoltDiameter { get; set; }
        public int? BoltCount { get; set; }
        public int? BoltRows { get; set; }
        public int? BoltVersion { get; set; }

        /// <summary>
        /// Координаты точек расположения болтов по Y (e1, p1–p10).
        /// Это координаты, не межболтовые расстояния.
        /// </summary>
        public double[] BoltCoordY { get; set; }

        /// <summary>
        /// Координаты точек расположения болтов по X (d1, d2).
        /// Это координаты, не межболтовые расстояния.
        /// </summary>
        public double[] BoltCoordX { get; set; }

        public double? BoltCoordZ { get; set; }

        // Сварка (катеты швов)
        public string[] WeldKf { get; set; }

        // Верхний уровень
        public string TableBrand { get; set; }

        /// <summary>Пояснения и общие положения из поля Explanations базы данных.</summary>
        public string Explanations { get; set; }
    }
}
