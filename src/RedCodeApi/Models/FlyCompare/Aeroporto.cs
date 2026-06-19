namespace RedCodeApi.Models.FlyCompare;

public class Aeroporto
{
    public int Id { get; set; }
    public string CodigoIATA { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string? Estado { get; set; }
    public string Pais { get; set; } = "Brasil";
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
