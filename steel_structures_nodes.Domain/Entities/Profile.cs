namespace Steel_structures_nodes_public_project.Domain.Entities;

/// <summary>
/// Профиль металлоконструкций
/// </summary>
public class Profile
{
    public Guid Id { get; set; }
    public Guid ConnectionGuid { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public double H { get; set; }
    public double B { get; set; }
    public double S { get; set; }
    public double T { get; set; }
    public double R1 { get; set; }
    public double R2 { get; set; }
    public double A { get; set; }
    public double P { get; set; }
    public double Iz { get; set; }
    public double Iy { get; set; }
    public double Ix { get; set; }
    public double Iv { get; set; }
    public double Iyz { get; set; }
    public double Wz { get; set; }
    public double Wy { get; set; }
    public double Wx { get; set; }
    public double Wvo { get; set; }
    public double Sz { get; set; }
    public double Sy { get; set; }
    public double Iz_lower { get; set; }
    public double Iy_lower { get; set; }
    public double Xo { get; set; }
    public double Yo { get; set; }
    public double Iu { get; set; }
    public double Iv_lower { get; set; }
}
