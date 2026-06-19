namespace RedCodeApi.Models.FlyCompare;

public class PrecoVoo
{
    public int Id { get; set; }
    public int VooId { get; set; }
    public decimal Preco { get; set; }
    public decimal Taxas { get; set; }
    public decimal PrecoTotal { get; set; }
    public string Moeda { get; set; } = "BRL";
    public string TipoTarifa { get; set; } = "Econômica";
    public bool BagagemIncluida { get; set; }
    public int? FranquiaBagagemKg { get; set; }
    public string UrlDestino { get; set; } = string.Empty;
    public string Fonte { get; set; } = string.Empty;
    public DateTime DataColeta { get; set; }
}
