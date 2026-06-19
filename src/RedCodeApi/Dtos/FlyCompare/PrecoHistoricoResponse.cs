namespace RedCodeApi.Dtos.FlyCompare;

public class PrecoHistoricoResponse
{
    public string CodigoVoo { get; set; } = string.Empty;
    public string Companhia { get; set; } = string.Empty;
    public List<PrecoHistoricoPonto> Precos { get; set; } = new();
}

public class PrecoHistoricoPonto
{
    public decimal Preco { get; set; }
    public DateTime DataColeta { get; set; }
    public string Fonte { get; set; } = string.Empty;
}
