using Dapper;
using Microsoft.Data.Sqlite;
using RedCodeApi.Dtos.FlyCompare;

namespace RedCodeApi.Services;

/// <summary>
/// Serviço de análise inteligente de preços de passagens aéreas (SPEC-034).
/// 
/// Algoritmo puramente estatístico — sem IA externa, sem custos, sem hardware especial.
/// Usa 4 fatores para calcular um score de 1 a 5 estrelas:
/// 
/// 1. Preço vs Média Histórica (peso 40%) — compara com preços dos últimos 30 dias
/// 2. Dias até à Partida (peso 25%) — janela ideal: 21-90 dias antes do voo
/// 3. Competitividade vs Concorrentes (peso 20%) — é o mais barato da mesma rota?
/// 4. Benefícios Inclusos (peso 15%) — bagagem incluída, tipo de tarifa
/// 
/// O score final é normalizado para 1-5 estrelas e acompanhado de uma
/// justificação textual em português.
/// </summary>
public class AnalisadorPrecosService
{
    private readonly string _connStr;
    private readonly ILogger<AnalisadorPrecosService> _logger;

    public AnalisadorPrecosService(string connStr, ILogger<AnalisadorPrecosService> logger)
    {
        _connStr = connStr;
        _logger = logger;
    }

    /// <summary>
    /// Analisa uma lista de resultados de busca e devolve uma análise
    /// individual para cada voo com score de 1 a 5 estrelas.
    /// </summary>
    /// <param name="resultados">Lista de voos encontrados na busca.</param>
    /// <param name="dataPartida">Data de partida da busca.</param>
    /// <returns>Lista de análises, uma por voo.</returns>
    public List<AnalisePrecoResponse> Analisar(List<ResultadoBusca> resultados, DateTime dataPartida)
    {
        if (resultados == null || resultados.Count == 0)
            return new List<AnalisePrecoResponse>();

        _logger.LogInformation(
            "[Analisador] Iniciando análise de {Qtd} voos para data {Data}",
            resultados.Count, dataPartida.ToString("yyyy-MM-dd"));

        var analises = new List<AnalisePrecoResponse>();

        // Obter dados históricos de todas as rotas relevantes
        var historicos = ObterHistoricos(resultados);

        // Obter menor preço entre os resultados atuais (para fator competitividade)
        var menorPrecoAtual = resultados.Min(r => r.PrecoTotal);

        // Dias até à partida
        var diasAtePartida = (dataPartida - DateTime.Today).Days;
        if (diasAtePartida < 0) diasAtePartida = 0;

        foreach (var voo in resultados)
        {
            var chaveRota = $"{voo.Origem}|{voo.Destino}";
            historicos.TryGetValue(chaveRota, out var stats);

            var fatores = new List<FatorScore>();

            // ── Fator 1: Preço vs Média Histórica (peso 40%) ──
            double fator1 = CalcularFatorPrecoVsHistorico(voo, stats, fatores);

            // ── Fator 2: Dias até à Partida (peso 25%) ──
            double fator2 = CalcularFatorDiasPartida(diasAtePartida, fatores);

            // ── Fator 3: Competitividade (peso 20%) ──
            double fator3 = CalcularFatorCompetitividade(voo, menorPrecoAtual, fatores);

            // ── Fator 4: Benefícios (peso 15%) ──
            double fator4 = CalcularFatorBeneficios(voo, fatores);

            // ── Score final (1-5) ──
            double scoreBruto = (fator1 * 0.40) + (fator2 * 0.25) + (fator3 * 0.20) + (fator4 * 0.15);
            int scoreFinal = (int)Math.Round(Math.Clamp(scoreBruto, 1.0, 5.0));

            var (label, emoji) = ObterLabelScore(scoreFinal);

            // Diferença percentual vs média histórica
            decimal? difPercentual = null;
            if (stats != null && stats.Media > 0)
            {
                difPercentual = Math.Round(
                    ((voo.PrecoTotal - stats.Media) / stats.Media) * 100, 1);
            }

            // Justificativa
            var justificativa = GerarJustificativa(scoreFinal, label, difPercentual, diasAtePartida, voo);

            analises.Add(new AnalisePrecoResponse
            {
                CodigoVoo = voo.CodigoVoo,
                Companhia = voo.Companhia,
                Rota = $"{voo.Origem} → {voo.Destino}",
                PrecoAtual = voo.PrecoTotal,
                Score = scoreFinal,
                LabelScore = label,
                EmojiScore = emoji,
                PrecoMedioHistorico = stats?.Media,
                MenorPrecoHistorico = stats?.Menor,
                DiferencaPercentualMedia = difPercentual,
                DiasAtePartida = Math.Max(0, diasAtePartida),
                EhMaisBarato = voo.PrecoTotal <= menorPrecoAtual,
                BagagemIncluida = voo.BagagemIncluida,
                Justificativa = justificativa,
                Fatores = fatores
            });
        }

        _logger.LogInformation(
            "[Analisador] Análise concluída: {Qtd} voos analisados",
            analises.Count);

        return analises;
    }

    // ═══════════════════════════════════════════════
    //  FATORES DE SCORE
    // ═══════════════════════════════════════════════

    /// <summary>
    /// Fator 1 (40%): Comparação com preço médio histórico.
    /// Quanto mais abaixo da média, melhor o score.
    /// </summary>
    private static double CalcularFatorPrecoVsHistorico(
        ResultadoBusca voo,
        EstatisticasRota? stats,
        List<FatorScore> fatores)
    {
        if (stats == null || stats.Media <= 0)
        {
            fatores.Add(new FatorScore
            {
                Nome = "Preço vs Média Histórica",
                Impacto = 2.5,
                Descricao = "Sem dados históricos suficientes para esta rota. Score neutro."
            });
            return 2.5; // Neutro se não há histórico
        }

        // Quantos desvios padrão abaixo/acima da média
        var desvios = stats.DesvioPadrao > 0
            ? (double)((voo.PrecoTotal - stats.Media) / stats.DesvioPadrao)
            : 0;

        double impacto;
        string descricao;

        if (desvios <= -2.0)
        {
            impacto = 5.0;
            descricao = $"Preço excecionalmente baixo! R$ {voo.PrecoTotal:N2} está muito abaixo da média histórica de R$ {stats.Media:N2}.";
        }
        else if (desvios <= -1.0)
        {
            impacto = 4.0;
            descricao = $"Preço abaixo da média histórica (R$ {stats.Media:N2}). Bom desconto.";
        }
        else if (desvios <= -0.3)
        {
            impacto = 3.5;
            descricao = $"Preço ligeiramente abaixo da média de R$ {stats.Media:N2}.";
        }
        else if (desvios <= 0.3)
        {
            impacto = 2.5;
            descricao = $"Preço dentro da média histórica de R$ {stats.Media:N2}.";
        }
        else if (desvios <= 1.5)
        {
            impacto = 1.5;
            descricao = $"Preço acima da média histórica de R$ {stats.Media:N2}. Considere aguardar.";
        }
        else
        {
            impacto = 1.0;
            descricao = $"Preço muito acima da média histórica de R$ {stats.Media:N2}. Não recomendamos comprar agora.";
        }

        fatores.Add(new FatorScore
        {
            Nome = "Preço vs Média Histórica",
            Impacto = Math.Round(impacto, 1),
            Descricao = descricao
        });

        return impacto;
    }

    /// <summary>
    /// Fator 2 (25%): Dias até à partida.
    /// Janela ideal: 21-90 dias. Muito perto = preços altos.
    /// Muito longe = incerteza.
    /// </summary>
    private static double CalcularFatorDiasPartida(int dias, List<FatorScore> fatores)
    {
        double impacto;
        string descricao;

        switch (dias)
        {
            case >= 60:
                impacto = 4.0;
                descricao = $"Ainda faltam {dias} dias. Boa antecedência — preços tendem a ser mais baixos.";
                break;
            case >= 30:
                impacto = 4.5;
                descricao = $"Faltam {dias} dias. Está na janela ideal de compra (30-60 dias antes).";
                break;
            case >= 21:
                impacto = 5.0;
                descricao = $"Faltam {dias} dias. Período ótimo para comprar! Preços equilibrados.";
                break;
            case >= 14:
                impacto = 3.5;
                descricao = $"Faltam {dias} dias. Ainda é um bom momento, mas preços podem começar a subir.";
                break;
            case >= 7:
                impacto = 2.5;
                descricao = $"Faltam apenas {dias} dias. Preços tendem a subir perto da data.";
                break;
            case >= 3:
                impacto = 1.5;
                descricao = $"Faltam só {dias} dias! Preços de última hora costumam ser mais altos.";
                break;
            default:
                impacto = 1.0;
                descricao = $"Voo é hoje ou amanhã ({dias} dias). Preços de última hora são os mais caros.";
                break;
        }

        fatores.Add(new FatorScore
        {
            Nome = "Antecedência da Compra",
            Impacto = Math.Round(impacto, 1),
            Descricao = descricao
        });

        return impacto;
    }

    /// <summary>
    /// Fator 3 (20%): Competitividade — é o voo mais barato entre os resultados?
    /// </summary>
    private static double CalcularFatorCompetitividade(
        ResultadoBusca voo,
        decimal menorPreco,
        List<FatorScore> fatores)
    {
        if (voo.PrecoTotal <= menorPreco)
        {
            fatores.Add(new FatorScore
            {
                Nome = "Competitividade",
                Impacto = 5.0,
                Descricao = "Este é o voo mais barato entre todos os resultados encontrados!"
            });
            return 5.0;
        }

        var difPercentual = (double)((voo.PrecoTotal - menorPreco) / menorPreco * 100);

        if (difPercentual <= 5)
        {
            fatores.Add(new FatorScore
            {
                Nome = "Competitividade",
                Impacto = 4.0,
                Descricao = $"Apenas {difPercentual:N0}% mais caro que a opção mais barata."
            });
            return 4.0;
        }

        if (difPercentual <= 15)
        {
            fatores.Add(new FatorScore
            {
                Nome = "Competitividade",
                Impacto = 2.5,
                Descricao = $"{difPercentual:N0}% mais caro que a opção mais barata."
            });
            return 2.5;
        }

        fatores.Add(new FatorScore
        {
            Nome = "Competitividade",
            Impacto = 1.0,
            Descricao = $"{difPercentual:N0}% mais caro que a opção mais barata. Considere alternativas."
        });
        return 1.0;
    }

    /// <summary>
    /// Fator 4 (15%): Benefícios inclusos — bagagem, tipo de tarifa.
    /// </summary>
    private static double CalcularFatorBeneficios(ResultadoBusca voo, List<FatorScore> fatores)
    {
        double impacto = 2.5; // Base neutra
        var beneficios = new List<string>();

        if (voo.BagagemIncluida)
        {
            impacto += 1.5;
            beneficios.Add("bagagem incluída");
        }
        else
        {
            impacto -= 0.5;
            beneficios.Add("bagagem não incluída");
        }

        if (voo.TipoTarifa?.ToLower() == "executiva" || voo.TipoTarifa?.ToLower() == "primeira classe")
        {
            impacto += 1.0;
            beneficios.Add($"tarifa {voo.TipoTarifa}");
        }

        if (voo.Paradas == 0)
        {
            impacto += 0.5;
            beneficios.Add("voo direto");
        }

        var descricao = beneficios.Count > 0
            ? $"Benefícios: {string.Join(", ", beneficios)}."
            : "Tarifa básica sem benefícios adicionais.";

        fatores.Add(new FatorScore
        {
            Nome = "Benefícios Inclusos",
            Impacto = Math.Round(Math.Clamp(impacto, 1.0, 5.0), 1),
            Descricao = descricao
        });

        return Math.Clamp(impacto, 1.0, 5.0);
    }

    // ═══════════════════════════════════════════════
    //  AUXILIARES
    // ═══════════════════════════════════════════════

    /// <summary>
    /// Obtém estatísticas históricas (média, menor preço, desvio padrão)
    /// dos últimos 30 dias para cada rota presente nos resultados.
    /// </summary>
    private Dictionary<string, EstatisticasRota> ObterHistoricos(List<ResultadoBusca> resultados)
    {
        var historicos = new Dictionary<string, EstatisticasRota>();

        try
        {
            using var db = new SqliteConnection(_connStr);

            foreach (var voo in resultados)
            {
                var chave = $"{voo.Origem}|{voo.Destino}";
                if (historicos.ContainsKey(chave))
                    continue;

                var stats = db.QueryFirstOrDefault<EstatisticasRota>(@"
                    SELECT
                        AVG(p.PrecoTotal) AS Media,
                        MIN(p.PrecoTotal) AS Menor,
                        COUNT(*) AS TotalAmostras
                    FROM Precos p
                    INNER JOIN Voos v ON p.VooId = v.Id
                    INNER JOIN Rotas r ON v.RotaId = r.Id
                    INNER JOIN Aeroportos a1 ON r.OrigemId = a1.Id
                    INNER JOIN Aeroportos a2 ON r.DestinoId = a2.Id
                    WHERE a1.CodigoIATA = @Origem
                      AND a2.CodigoIATA = @Destino
                      AND p.DataColeta >= datetime('now', '-30 days')",
                    new { Origem = voo.Origem, Destino = voo.Destino });

                if (stats != null && stats.TotalAmostras >= 3)
                {
                    // Calcular desvio padrão
                    var precos = db.Query<decimal>(@"
                        SELECT p.PrecoTotal
                        FROM Precos p
                        INNER JOIN Voos v ON p.VooId = v.Id
                        INNER JOIN Rotas r ON v.RotaId = r.Id
                        INNER JOIN Aeroportos a1 ON r.OrigemId = a1.Id
                        INNER JOIN Aeroportos a2 ON r.DestinoId = a2.Id
                        WHERE a1.CodigoIATA = @Origem
                          AND a2.CodigoIATA = @Destino
                          AND p.DataColeta >= datetime('now', '-30 days')",
                        new { Origem = voo.Origem, Destino = voo.Destino });

                    var listaPrecos = precos.ToList();
                    if (listaPrecos.Count > 1)
                    {
                        var media = (double)listaPrecos.Average();
                        var somaQuadrados = listaPrecos.Sum(p => Math.Pow((double)p - media, 2));
                        stats.DesvioPadrao = (decimal)Math.Sqrt(somaQuadrados / listaPrecos.Count);
                    }
                    else
                    {
                        stats.DesvioPadrao = stats.Media * 0.15m; // ~15% se só 1 amostra
                    }

                    historicos[chave] = stats;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Analisador] Erro ao obter dados históricos. Análise usará scores neutros.");
        }

        return historicos;
    }

    private static (string label, string emoji) ObterLabelScore(int score)
    {
        return score switch
        {
            5 => ("Excelente — Compre agora!", "⭐⭐⭐⭐⭐"),
            4 => ("Bom negócio", "⭐⭐⭐⭐"),
            3 => ("Preço normal", "⭐⭐⭐"),
            2 => ("Caro — Espere se possível", "⭐⭐"),
            _ => ("Muito caro — Não recomendamos", "⭐")
        };
    }

    private static string GerarJustificativa(
        int score,
        string label,
        decimal? difPercentual,
        int dias,
        ResultadoBusca voo)
    {
        var partes = new List<string>();

        if (difPercentual.HasValue)
        {
            var dir = difPercentual < 0 ? "abaixo" : "acima";
            partes.Add($"{Math.Abs(difPercentual.Value):N0}% {dir} da média histórica");
        }

        if (dias > 0)
            partes.Add($"faltam {dias} dias para o voo");

        if (voo.BagagemIncluida)
            partes.Add("bagagem incluída");

        if (voo.Paradas == 0)
            partes.Add("voo direto");

        partes.Add($"companhia {voo.Companhia}");

        var prefixo = score >= 4 ? "Recomendado! " :
                      score >= 3 ? "Aceitável. " :
                      "Evite. ";

        return prefixo + string.Join(" · ", partes) + ".";
    }

    /// <summary>
    /// DTO interno para estatísticas de rota do banco de dados.
    /// </summary>
    private class EstatisticasRota
    {
        public decimal Media { get; set; }
        public decimal Menor { get; set; }
        public decimal DesvioPadrao { get; set; }
        public int TotalAmostras { get; set; }
    }
}
