namespace RedCodeApi.Models.FlyCompare;

public class AlertaPreco
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public int RotaId { get; set; }
    public decimal PrecoAlvo { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; }
}
