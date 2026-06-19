namespace RedCodeFront.Models.FlyCompare;

/// <summary>
/// Modelo unificado de resultado de busca (LOW-03).
/// Inclui PrecoSemTaxas e Taxas para consistencia com o backend.
/// </summary>
public class ResultadoBusca
{
    public string CodigoVoo { get; set; } = string.Empty;
    public string Companhia { get; set; } = string.Empty;
    public string Origem { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public DateTime Partida { get; set; }
    public DateTime Chegada { get; set; }
    public int DuracaoMinutos { get; set; }
    public int Paradas { get; set; }
    public decimal PrecoTotal { get; set; }
    public decimal PrecoSemTaxas { get; set; }
    public decimal Taxas { get; set; }
    public string TipoTarifa { get; set; } = string.Empty;
    public bool BagagemIncluida { get; set; }
    public string UrlCompra { get; set; } = string.Empty;
    public string Fonte { get; set; } = string.Empty;
}
