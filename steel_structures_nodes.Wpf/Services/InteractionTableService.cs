using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using steel_structures_nodes.Domain.Contracts;
using steel_structures_nodes.Domain.Entities;
using steel_structures_nodes.Wpf.Models;

namespace steel_structures_nodes.Wpf.Services
{
    /// <summary>
    /// Сервис для работы с таблицами взаимодействия.
    /// Данные читаются из MongoDB (коллекция 'NodeDB', read-only) через IInteractionTableRepository.
    /// Task.Run используется для предотвращения deadlock в UI потоке WPF.
    /// </summary>
    internal sealed class InteractionTableService : IInteractionTableService
    {
        private readonly IInteractionTableLookupRepository _lookupRepository;
        private readonly IInteractionTableReadRepository _readRepository;
        private readonly IStandardNodeDataMapper _standardNodeDataMapper;

        public InteractionTableService(
            IInteractionTableLookupRepository lookupRepository,
            IInteractionTableReadRepository readRepository,
            IStandardNodeDataMapper standardNodeDataMapper)
        {
            _lookupRepository = lookupRepository ?? throw new ArgumentNullException(nameof(lookupRepository));
            _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
            _standardNodeDataMapper = standardNodeDataMapper ?? throw new ArgumentNullException(nameof(standardNodeDataMapper));
        }

        public string[] LoadDistinctNames()
        {
            return LoadLookupResult(() => _lookupRepository.GetDistinctNamesAsync());
        }

        public string[] LoadConnectionCodesByName(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length == 0) return Array.Empty<string>();
            return LoadLookupResult(() => _lookupRepository.GetConnectionCodesByNameAsync(name));
        }

        public string[] LoadConnectionCodesByNameAndBeam(string name, string beam)
        {
            name = (name ?? string.Empty).Trim();
            beam = (beam ?? string.Empty).Trim();
            if (name.Length == 0 || beam.Length == 0) return Array.Empty<string>();
            return LoadLookupResult(() => _lookupRepository.GetConnectionCodesByNameAndBeamAsync(name, beam));
        }

        public string[] LoadConnectionCodesByNameAndColumn(string name, string column)
        {
            name = (name ?? string.Empty).Trim();
            column = (column ?? string.Empty).Trim();
            if (name.Length == 0 || column.Length == 0) return Array.Empty<string>();
            return LoadLookupResult(() => _lookupRepository.GetConnectionCodesByNameAndColumnAsync(name, column));
        }

        public string[] LoadConnectionCodesByNameColumnAndBeam(string name, string column, string beam)
        {
            name = (name ?? string.Empty).Trim();
            column = (column ?? string.Empty).Trim();
            beam = (beam ?? string.Empty).Trim();
            if (name.Length == 0 || column.Length == 0 || beam.Length == 0) return Array.Empty<string>();
            return LoadLookupResult(() => _lookupRepository.GetConnectionCodesByNameColumnAndBeamAsync(name, column, beam));
        }

        public StandardNodeData LoadStandardNode(string name, string connectionCode)
        {
            name = (name ?? string.Empty).Trim();
            connectionCode = (connectionCode ?? string.Empty).Trim();
            if (name.Length == 0 || connectionCode.Length == 0) return null;

            var table = Task.Run(() => _readRepository.GetByNameAndConnectionCodeAsync(name, connectionCode)).GetAwaiter().GetResult();
            if (table == null) return null;

            return _standardNodeDataMapper.Map(table);
        }

        public string[] LoadDistinctProfileBeamsByName(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length == 0) return Array.Empty<string>();
            return LoadLookupResult(() => _lookupRepository.GetDistinctProfileBeamsByNameAsync(name));
        }

        public string[] LoadDistinctProfileBeamsByNameAndColumn(string name, string column)
        {
            name = (name ?? string.Empty).Trim();
            column = (column ?? string.Empty).Trim();
            if (name.Length == 0 || column.Length == 0) return Array.Empty<string>();
            return LoadLookupResult(() => _lookupRepository.GetDistinctProfileBeamsByNameAndColumnAsync(name, column));
        }

        public string[] LoadDistinctProfileColumnsByName(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length == 0) return Array.Empty<string>();
            return LoadLookupResult(() => _lookupRepository.GetDistinctProfileColumnsByNameAsync(name));
        }

        private static string[] LoadLookupResult(Func<Task<IReadOnlyList<string>>> loader)
        {
            return Task.Run(loader).GetAwaiter().GetResult().ToArray();
        }

    }
}
