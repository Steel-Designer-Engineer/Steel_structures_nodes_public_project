namespace steel_structures_nodes.Domain.Contracts;

/// <summary>
/// Репозиторий для работы с таблицами взаимодействия.
/// Сохраняет обратную совместимость и агрегирует более узкие контракты.
/// </summary>
public interface IInteractionTableRepository : IInteractionTableLookupRepository, IInteractionTableReadRepository
{
}
