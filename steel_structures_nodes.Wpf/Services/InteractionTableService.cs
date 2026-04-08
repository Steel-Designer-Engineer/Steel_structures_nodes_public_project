using System;
using System.Linq;
using System.Threading.Tasks;
using Steel_structures_nodes_public_project.Domain.Entities;
using Steel_structures_nodes_public_project.Domain.Repositories;
using Steel_structures_nodes_public_project.Wpf.Models;

namespace Steel_structures_nodes_public_project.Wpf.Services
{
    /// <summary>
    /// Сервис для работы с таблицами взаимодействия.
    /// Данные читаются из MongoDB (коллекция 'all_node') через IInteractionTableRepository.
    /// Task.Run используется для предотвращения deadlock в UI потоке WPF.
    /// </summary>
    internal sealed class InteractionTableService
    {
        private readonly IInteractionTableRepository _repository;

        public InteractionTableService(IInteractionTableRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public string[] LoadDistinctNames()
        {
            return Task.Run(() => _repository.GetDistinctNamesAsync()).GetAwaiter().GetResult().ToArray();
        }

        public string[] LoadConnectionCodesByName(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length == 0) return Array.Empty<string>();
            return Task.Run(() => _repository.GetConnectionCodesByNameAsync(name)).GetAwaiter().GetResult().ToArray();
        }

        public string[] LoadConnectionCodesByNameAndBeam(string name, string beam)
        {
            name = (name ?? string.Empty).Trim();
            beam = (beam ?? string.Empty).Trim();
            if (name.Length == 0 || beam.Length == 0) return Array.Empty<string>();
            return Task.Run(() => _repository.GetConnectionCodesByNameAndBeamAsync(name, beam)).GetAwaiter().GetResult().ToArray();
        }

        public string[] LoadConnectionCodesByNameAndColumn(string name, string column)
        {
            name = (name ?? string.Empty).Trim();
            column = (column ?? string.Empty).Trim();
            if (name.Length == 0 || column.Length == 0) return Array.Empty<string>();
            return Task.Run(() => _repository.GetConnectionCodesByNameAndColumnAsync(name, column)).GetAwaiter().GetResult().ToArray();
        }

        public string[] LoadConnectionCodesByNameColumnAndBeam(string name, string column, string beam)
        {
            name = (name ?? string.Empty).Trim();
            column = (column ?? string.Empty).Trim();
            beam = (beam ?? string.Empty).Trim();
            if (name.Length == 0 || column.Length == 0 || beam.Length == 0) return Array.Empty<string>();
            return Task.Run(() => _repository.GetConnectionCodesByNameColumnAndBeamAsync(name, column, beam)).GetAwaiter().GetResult().ToArray();
        }

        public StandardNodeData LoadStandardNode(string name, string connectionCode)
        {
            name = (name ?? string.Empty).Trim();
            connectionCode = (connectionCode ?? string.Empty).Trim();
            if (name.Length == 0 || connectionCode.Length == 0) return null;

            var table = Task.Run(() => _repository.GetByNameAndConnectionCodeAsync(name, connectionCode)).GetAwaiter().GetResult();
            if (table == null) return null;

            return MapToStandardNodeData(table);
        }

        public string[] LoadDistinctProfileBeamsByName(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length == 0) return Array.Empty<string>();
            return Task.Run(() => _repository.GetDistinctProfileBeamsByNameAsync(name)).GetAwaiter().GetResult().ToArray();
        }

        public string[] LoadDistinctProfileBeamsByNameAndColumn(string name, string column)
        {
            name = (name ?? string.Empty).Trim();
            column = (column ?? string.Empty).Trim();
            if (name.Length == 0 || column.Length == 0) return Array.Empty<string>();
            return Task.Run(() => _repository.GetDistinctProfileBeamsByNameAndColumnAsync(name, column)).GetAwaiter().GetResult().ToArray();
        }

        public string[] LoadDistinctProfileColumnsByName(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length == 0) return Array.Empty<string>();
            return Task.Run(() => _repository.GetDistinctProfileColumnsByNameAsync(name)).GetAwaiter().GetResult().ToArray();
        }

        private static StandardNodeData MapToStandardNodeData(InteractionTable table)
        {
            var boltsY = table.Bolts?.CoordinatesBolts?.Y;
            var boltsX = table.Bolts?.CoordinatesBolts?.X;
            var welds = table.Welds;

            return new StandardNodeData
            {
                ProfileBeam = table.Geometry?.Beam?.ProfileBeam,
                ProfileColumn = table.Geometry?.Column?.ProfileColumn,
                Sj = table.Stiffness?.Sj,
                Sjo = table.Stiffness?.Sjo,
                Variable = table.Variable,
                N = table.InternalForces?.N,
                Nt = table.InternalForces?.Nt,
                Nc = table.InternalForces?.Nc,
                My = table.InternalForces?.My,
                Mz = table.InternalForces?.Mz,
                Mx = table.InternalForces?.Mx,
                Mw = table.InternalForces?.Mw,
                T = table.InternalForces?.T,
                Mneg = table.InternalForces?.Mneg,
                Qy = table.InternalForces?.Qy,
                Qz = table.InternalForces?.Qz,
                Alpha = table.Coefficients?.Alpha,
                Beta = table.Coefficients?.Beta,
                Gamma = table.Coefficients?.Gamma,
                Delta = table.Coefficients?.Delta,
                Epsilon = table.Coefficients?.Epsilon,
                Lambda = table.Coefficients?.Lambda,
                SectionH = table.Geometry?.Beam?.Beam_H,
                SectionB = table.Geometry?.Beam?.Beam_B,
                SectionS = table.Geometry?.Beam?.Beam_s,
                SectionT = table.Geometry?.Beam?.Beam_t,
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

                // Геометрия колонны
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

                // Пластина
                PlateH = table.Geometry?.Plate?.Plate_H,
                PlateB = table.Geometry?.Plate?.Plate_B,
                PlateT = table.Geometry?.Plate?.Plate_t,

                // Фланец
                FlangeLb = table.Geometry?.Flange?.Flange_Lb,
                FlangeH = table.Geometry?.Flange?.Flange_H,
                FlangeB = table.Geometry?.Flange?.Flange_B,
                FlangeT = table.Geometry?.Flange?.Flange_t,

                // Рёбра жёсткости
                StiffTr1 = table.Geometry?.Stiff?.Stiff_tr1,
                StiffTr2 = table.Geometry?.Stiff?.Stiff_tr2,
                StiffTbp = table.Geometry?.Stiff?.Stiff_tbp,
                StiffTg = table.Geometry?.Stiff?.Stiff_tg,
                StiffTf = table.Geometry?.Stiff?.Stiff_tf,
                StiffLh = table.Geometry?.Stiff?.Stiff_Lh,
                StiffHh = table.Geometry?.Stiff?.Stiff_Hh,
                StiffTwp = table.Geometry?.Stiff?.Stiff_twp,

                // Болты
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

                // Сварка
                WeldKf = welds != null
                    ? new[] { welds.kf1, welds.kf2, welds.kf3, welds.kf4, welds.kf5,
                              welds.kf6, welds.kf7, welds.kf8, welds.kf9, welds.kf10 }
                    : null,

                // Верхний уровень
                TableBrand = table.TableBrand,
                Explanations = table.Explanations,
            };
        }
    }
}
