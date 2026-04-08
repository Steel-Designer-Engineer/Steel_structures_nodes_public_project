using CommunityToolkit.Mvvm.ComponentModel;

namespace Steel_structures_nodes_public_project.Maui.ViewModels;

/// <summary>
/// DTO для отображения примечания к узлу в UI.
/// </summary>
public partial class NodeNoteDisplay : ObservableObject
{
    public Guid Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
}
