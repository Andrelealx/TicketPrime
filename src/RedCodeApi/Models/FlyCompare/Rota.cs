namespace RedCodeApi.Models.FlyCompare;

public class Rota
{
    public int Id { get; set; }
    public int OrigemId { get; set; }
    public int DestinoId { get; set; }

    // Propriedades de navegacao (populadas via JOIN)
    public string? OrigemCodigo { get; set; }
    public string? DestinoCodigo { get; set; }
    public string? OrigemCidade { get; set; }
    public string? DestinoCidade { get; set; }
}
