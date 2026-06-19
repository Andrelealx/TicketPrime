namespace RedCodeFront.Models.FlyCompare;

/// <summary>
/// Modelo do frontend para resposta da análise de preços.
/// Espelha o DTO da API: Dtos/FlyCompare/AnalisePrecoResponse.cs
/// </summary>
public class AnalisePrecoResponse
{
    public string CodigoVoo { get; set; } = string.Empty;
    public string Companhia { get; set; } = string.Empty;
    public string Rota { get; set; } = string.Empty;
    public decimal PrecoAtual { get; set; }
    public int Score { get; set; }
    public string LabelScore { get; set; } = string.Empty;
    public string EmojiScore { get; set; } = string.Empty;
    public decimal? PrecoMedioHistorico { get; set; }
    public decimal? MenorPrecoHistorico { get; set; }
    public decimal? DiferencaPercentualMedia { get; set; }
    public int DiasAtePartida { get; set; }
    public bool EhMaisBarato { get; set; }
    public bool BagagemIncluida { get; set; }
    public string Justificativa { get; set; } = string.Empty;
}
