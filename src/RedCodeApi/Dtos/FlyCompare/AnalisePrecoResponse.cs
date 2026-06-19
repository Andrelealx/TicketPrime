namespace RedCodeApi.Dtos.FlyCompare;

/// <summary>
/// Resposta do endpoint de análise de preços.
/// Contém o score (1-5 estrelas), label descritivo,
/// métricas de comparação e justificação textual.
/// </summary>
public class AnalisePrecoResponse
{
    /// <summary>Código do voo analisado.</summary>
    public string CodigoVoo { get; set; } = string.Empty;

    /// <summary>Nome da companhia aérea.</summary>
    public string Companhia { get; set; } = string.Empty;

    /// <summary>Origem → Destino.</summary>
    public string Rota { get; set; } = string.Empty;

    /// <summary>Preço total atual do voo.</summary>
    public decimal PrecoAtual { get; set; }

    /// <summary>
    /// Score de 1 a 5 estrelas.
    /// 5 = Excelente (comprar agora),
    /// 1 = Muito caro (evitar).
    /// </summary>
    public int Score { get; set; }

    /// <summary>Label descritivo do score.</summary>
    public string LabelScore { get; set; } = string.Empty;

    /// <summary>Emoji representativo do score.</summary>
    public string EmojiScore { get; set; } = string.Empty;

    /// <summary>Preço médio histórico da rota (últimos 30 dias).</summary>
    public decimal? PrecoMedioHistorico { get; set; }

    /// <summary>Menor preço histórico registado para esta rota.</summary>
    public decimal? MenorPrecoHistorico { get; set; }

    /// <summary>Percentagem acima/abaixo da média (negativo = abaixo).</summary>
    public decimal? DiferencaPercentualMedia { get; set; }

    /// <summary>Dias até à data do voo.</summary>
    public int DiasAtePartida { get; set; }

    /// <summary>É a opção mais barata entre todos os resultados da busca?</summary>
    public bool EhMaisBarato { get; set; }

    /// <summary>Tem bagagem incluída na tarifa?</summary>
    public bool BagagemIncluida { get; set; }

    /// <summary>Justificação textual do score atribuído.</summary>
    public string Justificativa { get; set; } = string.Empty;

    /// <summary>Fatores que contribuíram para o score.</summary>
    public List<FatorScore> Fatores { get; set; } = new();
}

/// <summary>
/// Fator individual que contribuiu para o score.
/// </summary>
public class FatorScore
{
    /// <summary>Nome do fator (ex: "Preço vs Média Histórica").</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Impacto no score (positivo = bom, negativo = mau).</summary>
    public double Impacto { get; set; }

    /// <summary>Descrição do impacto deste fator.</summary>
    public string Descricao { get; set; } = string.Empty;
}
