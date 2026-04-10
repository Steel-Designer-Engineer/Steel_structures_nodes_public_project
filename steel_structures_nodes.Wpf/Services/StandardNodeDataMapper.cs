using steel_structures_nodes.Domain.Entities;
using steel_structures_nodes.Wpf.Models;

namespace steel_structures_nodes.Wpf.Services
{
    internal sealed class StandardNodeDataMapper : IStandardNodeDataMapper
    {
        public StandardNodeData Map(InteractionTable table)
        {
            var boltsY = table.Bolts?.CoordinatesBolts?.Y;
            var boltsX = table.Bolts?.CoordinatesBolts?.X;
            var welds = table.Welds;

            return new StandardNodeData
            {
                TypeNode = table.TypeNode,
                ProfileBeam = table.Geometry?.Beam?.ProfileBeam,
                ProfileColumn = table.Geometry?.Column?.ProfileColumn,
                Sj = table.Stiffness?.Sj,
                Sjo = table.Stiffness?.Sjo,
                Variable = table.Variable,
                N = table.InternalForces?.N,
                Nt = table.InternalForces?.Nt,
                Nc = table.InternalForces?.Nc,
                Qy = table.InternalForces?.Qy,
                Qz = table.InternalForces?.Qz,
                My = table.InternalForces?.My,
                Mz = table.InternalForces?.Mz,
                Mx = table.InternalForces?.Mx,
                Mw = table.InternalForces?.Mw,
                T = table.InternalForces?.T,
                Mneg = table.InternalForces?.Mneg,
                Alpha = table.Coefficients?.Alpha,
                Beta = table.Coefficients?.Beta,
                Gamma = table.Coefficients?.Gamma,
                Delta = table.Coefficients?.Delta,
                Epsilon = table.Coefficients?.Epsilon,
                Lambda = table.Coefficients?.Lambda,
                BeamH = table.Geometry?.Beam?.Beam_H,
                BeamB = table.Geometry?.Beam?.Beam_B,
                BeamS = table.Geometry?.Beam?.Beam_s,
                BeamT = table.Geometry?.Beam?.Beam_t,
                BeamA = table.Geometry?.Beam?.Beam_A,
                BeamP = table.Geometry?.Beam?.Beam_P,
                BeamIz = table.Geometry?.Beam?.Beam_Iz,
                BeamIy = table.Geometry?.Beam?.Beam_Iy,
                BeamIx = table.Geometry?.Beam?.Beam_Ix,
                BeamWz = table.Geometry?.Beam?.Beam_Wz,
                BeamWy = table.Geometry?.Beam?.Beam_Wy,
                BeamWx = table.Geometry?.Beam?.Beam_Wx,
                BeamSz = table.Geometry?.Beam?.Beam_Sz,
                BeamSy = table.Geometry?.Beam?.Beam_Sy,
                Beamiz = table.Geometry?.Beam?.Beam_iz,
                Beamiy = table.Geometry?.Beam?.Beam_iy,
                BeamXo = table.Geometry?.Beam?.Beam_xo,
                ColumnH = table.Geometry?.Column?.Column_H,
                ColumnB = table.Geometry?.Column?.Column_B,
                ColumnS = table.Geometry?.Column?.Column_s,
                ColumnT = table.Geometry?.Column?.Column_t,
                ColumnA = table.Geometry?.Column?.Column_A,
                ColumnP = table.Geometry?.Column?.Column_P,
                ColumnIz = table.Geometry?.Column?.Column_Iz,
                ColumnIy = table.Geometry?.Column?.Column_Iy,
                ColumnIx = table.Geometry?.Column?.Column_Ix,
                ColumnWz = table.Geometry?.Column?.Column_Wz,
                ColumnWy = table.Geometry?.Column?.Column_Wy,
                ColumnWx = table.Geometry?.Column?.Column_Wx,
                ColumnSz = table.Geometry?.Column?.Column_Sz,
                ColumnSy = table.Geometry?.Column?.Column_Sy,
                Columniz = table.Geometry?.Column?.Column_iz,
                Columniy = table.Geometry?.Column?.Column_iy,
                ColumnXo = table.Geometry?.Column?.Column_xo,
                ColumnYo = table.Geometry?.Column?.Column_yo,
                PlateH = table.Geometry?.Plate?.Plate_H,
                PlateB = table.Geometry?.Plate?.Plate_B,
                PlateT = table.Geometry?.Plate?.Plate_t,
                FlangeLb = table.Geometry?.Flange?.Flange_Lb,
                FlangeH = table.Geometry?.Flange?.Flange_H,
                FlangeB = table.Geometry?.Flange?.Flange_B,
                FlangeT = table.Geometry?.Flange?.Flange_t,
                StiffTr1 = table.Geometry?.Stiff?.Stiff_tr1,
                StiffTr2 = table.Geometry?.Stiff?.Stiff_tr2,
                StiffTbp = table.Geometry?.Stiff?.Stiff_tbp,
                StiffTg = table.Geometry?.Stiff?.Stiff_tg,
                StiffTf = table.Geometry?.Stiff?.Stiff_tf,
                StiffLh = table.Geometry?.Stiff?.Stiff_Lh,
                StiffHh = table.Geometry?.Stiff?.Stiff_Hh,
                StiffTwp = table.Geometry?.Stiff?.Stiff_twp,
                BoltDiameter = table.Bolts?.DiameterBolt?.F,
                BoltCount = table.Bolts?.CountBolt?.Bolts_Nb,
                BoltRows = table.Bolts?.BoltRow?.N_Rows,
                BoltVersion = table.Bolts?.Option?.version,
                BoltCoordY = boltsY != null
                    ? new[] { boltsY.Bolt1_e1, boltsY.Bolt2_p1, boltsY.Bolt3_p2, boltsY.Bolt4_p3,
                              boltsY.Bolt5_p4, boltsY.Bolt6_p5, boltsY.Bolt7_p6, boltsY.Bolt8_p7,
                              boltsY.Bolt9_p8, boltsY.Bolt10_p9, boltsY.Bolt11_p10 }
                    : null,
                BoltCoordX = boltsX != null
                    ? new[] { boltsX.d1, boltsX.d2 }
                    : null,
                BoltCoordZ = table.Bolts?.CoordinatesBolts?.Z?.BoltCoordinateZ,
                WeldKf = welds != null
                    ? new[] { welds.kf1, welds.kf2, welds.kf3, welds.kf4, welds.kf5,
                              welds.kf6, welds.kf7, welds.kf8, welds.kf9, welds.kf10 }
                    : null,
                TableBrand = table.TableBrand,
                Explanations = table.Explanations,
            };
        }
    }
}
