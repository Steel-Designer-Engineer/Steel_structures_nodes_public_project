namespace Steel_structures_nodes_public_project.Domain.Entities;

/// <summary>
/// Таблица взаимодействия узлов
/// </summary>
public class InteractionTable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ConnectionCode { get; set; } = string.Empty;
    public double Variable { get; set; }
    public string TableBrand { get; set; } = string.Empty;

    /// <summary>Пояснения и общие положения из базы данных узла.</summary>
    public string Explanations { get; set; } = string.Empty;

    public StiffnessData Stiffness { get; set; } = new();
    public GeometryData Geometry { get; set; } = new();
    public BoltsData Bolts { get; set; } = new();
    public WeldsData Welds { get; set; } = new();
    public InternalForcesData InternalForces { get; set; } = new();
    public CoefficientsData Coefficients { get; set; } = new();
}

public class StiffnessData
{
    public double Sj { get; set; }
    public double Sjo { get; set; }
}

public class GeometryData
{
    public BeamData Beam { get; set; } = new();
    public ColumnData Column { get; set; } = new();
    public PlateData Plate { get; set; } = new();
    public FlangeData Flange { get; set; } = new();
    public StiffData Stiff { get; set; } = new();
}

public class BeamData
{
    public string ProfileBeam { get; set; } = string.Empty;
    public double Beam_H { get; set; }
    public double Beam_B { get; set; }
    public double Beam_s { get; set; }
    public double Beam_t { get; set; }
    public double Beam_A { get; set; }
    public double Beam_P { get; set; }
    public double Beam_Iz { get; set; }
    public double Beam_Iy { get; set; }
    public double Beam_Ix { get; set; }
    public double Beam_Wz { get; set; }
    public double Beam_Wy { get; set; }
    public double Beam_Wx { get; set; }
    public double Beam_Sz { get; set; }
    public double Beam_Sy { get; set; }
    public double Beam_iz { get; set; }
    public double Beam_iy { get; set; }
    public double Beam_xo { get; set; }
}

public class ColumnData
{
    public string ProfileColumn { get; set; } = string.Empty;
    public double Column_H { get; set; }
    public double Column_B { get; set; }
    public double Column_s { get; set; }
    public double Column_t { get; set; }
    public double Column_A { get; set; }
    public double Column_P { get; set; }
    public double Column_Iz { get; set; }
    public double Column_Iy { get; set; }
    public double Column_Ix { get; set; }
    public double Column_Wz { get; set; }
    public double Column_Wy { get; set; }
    public double Column_Wx { get; set; }
    public double Column_Sz { get; set; }
    public double Column_Sy { get; set; }
    public double Column_iz { get; set; }
    public double Column_iy { get; set; }
    public double Column_xo { get; set; }
    public double Column_yo { get; set; }
}

public class PlateData
{
    public double Plate_H { get; set; }
    public double Plate_B { get; set; }
    public double Plate_t { get; set; }
}

public class FlangeData
{
    public double Flange_Lb { get; set; }
    public double Flange_H { get; set; }
    public double Flange_B { get; set; }
    public double Flange_t { get; set; }
}

public class StiffData
{
    public double Stiff_tbp { get; set; }
    public double Stiff_tg { get; set; }
    public double Stiff_tf { get; set; }
    public double Stiff_Lh { get; set; }
    public double Stiff_Hh { get; set; }
    public double Stiff_tr1 { get; set; }
    public double Stiff_tr2 { get; set; }
    public double Stiff_twp { get; set; }
}

public class BoltsData
{
    public BoltOptionData Option { get; set; } = new();
    public BoltDiameterData DiameterBolt { get; set; } = new();
    public BoltCountData CountBolt { get; set; } = new();
    public BoltRowData BoltRow { get; set; } = new();
    public BoltCoordinatesData CoordinatesBolts { get; set; } = new();
}

public class BoltOptionData
{
    public int version { get; set; }
}

public class BoltDiameterData
{
    public double F { get; set; }
}

public class BoltCountData
{
    public int Bolts_Nb { get; set; }
}

public class BoltRowData
{
    public int N_Rows { get; set; }
}

public class BoltCoordinatesData
{
    public BoltCoordinatesY Y { get; set; } = new();
    public BoltCoordinatesX X { get; set; } = new();
    public BoltCoordinatesZ Z { get; set; } = new();
}

/// <summary>
/// Координаты точек расположения болтов на пластине по оси Y.
/// e1, p1–p10 — это координаты, не межболтовые расстояния.
/// </summary>
public class BoltCoordinatesY
{
    public double Bolt1_e1 { get; set; }
    public double Bolt2_p1 { get; set; }
    public double Bolt3_p2 { get; set; }
    public double Bolt4_p3 { get; set; }
    public double Bolt5_p4 { get; set; }
    public double Bolt6_p5 { get; set; }
    public double Bolt7_p6 { get; set; }
    public double Bolt8_p7 { get; set; }
    public double Bolt9_p8 { get; set; }
    public double Bolt10_p9 { get; set; }
    public double Bolt11_p10 { get; set; }
}

/// <summary>
/// Координаты точек расположения болтов на пластине по оси X.
/// d1, d2 — это координаты, не межболтовые расстояния.
/// </summary>
public class BoltCoordinatesX
{
    public double d1 { get; set; }
    public double d2 { get; set; }
}

public class BoltCoordinatesZ
{
    public double BoltCoordinateZ { get; set; }
}

public class WeldsData
{
    public double kf1 { get; set; }
    public double kf2 { get; set; }
    public double kf3 { get; set; }
    public double kf4 { get; set; }
    public double kf5 { get; set; }
    public double kf6 { get; set; }
    public double kf7 { get; set; }
    public double kf8 { get; set; }
    public double kf9 { get; set; }
    public double kf10 { get; set; }
}

public class InternalForcesData
{
    public double N { get; set; }
    public double Nt { get; set; }
    public double Nc { get; set; }
    public double My { get; set; }
    public double Mz { get; set; }
    public double Mx { get; set; }
    public double Mw { get; set; }
    public double T { get; set; }
    public double Mneg { get; set; }
    public double Qy { get; set; }
    public double Qz { get; set; }
}

public class CoefficientsData
{
    public double Alpha { get; set; }
    public double Beta { get; set; }
    public double Gamma { get; set; }
    public double Delta { get; set; }
    public double Epsilon { get; set; }
    public double Lambda { get; set; }
}
