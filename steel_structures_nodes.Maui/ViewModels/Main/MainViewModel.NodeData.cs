using steel_structures_nodes.Domain.Entities;

namespace steel_structures_nodes.Maui.ViewModels;

public partial class MainViewModel
{
    private void ApplyLoadedNodeData(InteractionTable? table, string connectionCode)
    {
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

    private void SynchronizeSelectionsWithLoadedNode(InteractionTable table)
    {
        var nodeColumn = table.Geometry?.Column?.ProfileColumn;
        var nodeBeam = table.Geometry?.Beam?.ProfileBeam;

        _suppressCascade = true;
        if (!string.IsNullOrWhiteSpace(nodeColumn) && ProfileColumns.Contains(nodeColumn))
            SelectedProfileColumn = nodeColumn;
        if (!string.IsNullOrWhiteSpace(nodeBeam) && ProfileBeams.Contains(nodeBeam))
            SelectedProfileBeam = nodeBeam;
        _suppressCascade = false;
    }

    private void ApplyHeaderData(InteractionTable table)
    {
        TypeNode = table.TypeNode;
        TableBrand = table.TableBrand;
    }

    private void ApplyExplanationData(InteractionTable table)
    {
        NodeExplanation = string.IsNullOrWhiteSpace(table.Explanations) ? null : table.Explanations;
        HasExplanation = !string.IsNullOrWhiteSpace(table.Explanations);
    }

    private void ApplyForcesData(InternalForcesData forces)
    {
        _currentForces = forces;
        NtValue = Format(forces.Nt);
        NcValue = Format(forces.Nc);
        NValue = Format(forces.N);
        QyValue = Format(forces.Qy);
        QzValue = Format(forces.Qz);
        MxValue = Format(forces.Mx);
        MyValue = Format(forces.My);
        MzValue = Format(forces.Mz);
        MwValue = Format(forces.Mw);
        MnegValue = Format(forces.Mneg);

        UpdateCapacityChart(forces, _currentActual, GetUserForces());
    }

    private void ApplyStiffnessData(StiffnessData stiffness)
    {
        SjValue = Format(stiffness.Sj);
        SjoValue = Format(stiffness.Sjo);
    }

    private void ApplyCoefficientData(CoefficientsData coefficients)
    {
        AlphaValue = Format(coefficients.Alpha);
        BetaValue = Format(coefficients.Beta);
        GammaValue = Format(coefficients.Gamma);
        DeltaValue = Format(coefficients.Delta);
        EpsilonValue = Format(coefficients.Epsilon);
        LambdaValue = Format(coefficients.Lambda);
    }

    private void ApplyBeamGeometry(BeamData beam)
    {
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
    }

    private void ApplyColumnGeometry(ColumnData column)
    {
        ColumnProfile = column.ProfileColumn;
        ColumnH = Format(column.Column_H);
        ColumnB = Format(column.Column_B);
        ColumnS = Format(column.Column_s);
        ColumnT = Format(column.Column_t);
        ColumnA = Format(column.Column_A);
        ColumnP = Format(column.Column_P);
        ColumnIz = Format(column.Column_Iz);
        ColumnIy = Format(column.Column_Iy);
        ColumnIx = Format(column.Column_Ix);
        ColumnWz = Format(column.Column_Wz);
        ColumnWy = Format(column.Column_Wy);
        ColumnWx = Format(column.Column_Wx);
        ColumnSz = Format(column.Column_Sz);
        ColumnSy = Format(column.Column_Sy);
        Columniz = Format(column.Column_iz);
        Columniy = Format(column.Column_iy);
        ColumnXo = Format(column.Column_xo);
        ColumnYo = Format(column.Column_yo);
    }

    private void ApplyPlateData(PlateData plate)
    {
        PlateH = Format(plate.Plate_H);
        PlateB = Format(plate.Plate_B);
        PlateT = Format(plate.Plate_t);
    }

    private void ApplyFlangeData(FlangeData flange)
    {
        FlangeH = Format(flange.Flange_H);
        FlangeB = Format(flange.Flange_B);
        FlangeT = Format(flange.Flange_t);
        FlangeLb = Format(flange.Flange_Lb);
    }

    private void ApplyStiffenerData(StiffData stiffener)
    {
        StiffTr1 = Format(stiffener.Stiff_tr1);
        StiffTr2 = Format(stiffener.Stiff_tr2);
        StiffTbp = Format(stiffener.Stiff_tbp);
        StiffTg = Format(stiffener.Stiff_tg);
        StiffTf = Format(stiffener.Stiff_tf);
        StiffLh = Format(stiffener.Stiff_Lh);
        StiffHh = Format(stiffener.Stiff_Hh);
        StiffTwp = Format(stiffener.Stiff_twp);
    }

    private void ApplyBoltData(BoltsData bolts)
    {
        BoltCount = bolts.CountBolt.Bolts_Nb.ToString();
        BoltRows = bolts.BoltRow.N_Rows.ToString();
        BoltDiameter = Format(bolts.DiameterBolt.F);
        BoltVersion = bolts.Option.version.ToString();
        BoltCoordZ = Format(bolts.CoordinatesBolts.Z.BoltCoordinateZ);

        var coordinatesY = bolts.CoordinatesBolts.Y;
        BoltE1 = Format(coordinatesY.Bolt1_e1);
        BoltP1 = Format(coordinatesY.Bolt2_p1);
        BoltP2 = Format(coordinatesY.Bolt3_p2);
        BoltP3 = Format(coordinatesY.Bolt4_p3);
        BoltP4 = Format(coordinatesY.Bolt5_p4);
        BoltP5 = Format(coordinatesY.Bolt6_p5);
        BoltP6 = Format(coordinatesY.Bolt7_p6);
        BoltP7 = Format(coordinatesY.Bolt8_p7);
        BoltP8 = Format(coordinatesY.Bolt9_p8);
        BoltP9 = Format(coordinatesY.Bolt10_p9);
        BoltP10 = Format(coordinatesY.Bolt11_p10);

        var coordinatesX = bolts.CoordinatesBolts.X;
        BoltD1 = Format(coordinatesX.d1);
        BoltD2 = Format(coordinatesX.d2);
    }

    private void ApplyWeldData(WeldsData welds)
    {
        WeldKf1 = Format(welds.kf1);
        WeldKf2 = Format(welds.kf2);
        WeldKf3 = Format(welds.kf3);
        WeldKf4 = Format(welds.kf4);
        WeldKf5 = Format(welds.kf5);
        WeldKf6 = Format(welds.kf6);
        WeldKf7 = Format(welds.kf7);
        WeldKf8 = Format(welds.kf8);
        WeldKf9 = Format(welds.kf9);
        WeldKf10 = Format(welds.kf10);
    }
}
