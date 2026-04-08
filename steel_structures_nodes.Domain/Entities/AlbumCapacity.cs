namespace Steel_structures_nodes_public_project.Domain.Entities;

/// <summary>
/// Несущая способность узла из альбома
/// </summary>
public class AlbumCapacity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public double AlbumN { get; set; }
    public double AlbumNt { get; set; }
    public double AlbumNc { get; set; }
    public double AlbumQy { get; set; }
    public double AlbumQz { get; set; }
    public double AlbumT { get; set; }
    public double AlbumMy { get; set; }
    public double AlbumMx { get; set; }
    public double AlbumMz { get; set; }
    public double AlbumMw { get; set; }
    public double AlbumPsi { get; set; }
}
