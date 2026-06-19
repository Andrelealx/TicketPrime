namespace RedCodeApi.Dtos.FlyCompare;

public class AlertaRequest
{
    public string Email { get; set; } = string.Empty;
    public string Origem { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public decimal PrecoAlvo { get; set; }
}
