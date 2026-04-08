namespace Steel_structures_nodes_public_project.Domain.Entities;

/// <summary>
/// Примечание / пояснение к узлу соединения.
/// </summary>
public class NodeNote
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Имя узла (Name из InteractionTable).</summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>Текст примечания.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Дата создания (UTC).</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
