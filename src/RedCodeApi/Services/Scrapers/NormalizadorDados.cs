using RedCodeApi.Dtos.FlyCompare;

namespace RedCodeApi.Services.Scrapers;

/// <summary>
/// Servico responsavel por normalizar, deduplicar e ordenar os resultados
/// de busca provenientes de diferentes scrapers (SPEC-014).
///
/// Responsabilidades:
/// - Deduplicar voos com mesmo codigo (fica o mais barato)
/// - Identificar e remover outliers de preco (acima de 3x o desvio padrao)
/// - Ordenar por preco total crescente
/// - Garantir que todos os campos estejam preenchidos corretamente
/// </summary>
public class NormalizadorDados
{
    private readonly ILogger<NormalizadorDados> _logger;

    public NormalizadorDados(ILogger<NormalizadorDados> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Normaliza uma lista de resultados de busca.
    /// </summary>
    /// <param name="resultados">Lista bruta de resultados de todos os scrapers.</param>
    /// <returns>Lista normalizada, deduplicada e ordenada por preco.</returns>
    public List<ResultadoBusca> Normalizar(List<ResultadoBusca> resultados)
    {
        if (resultados == null || resultados.Count == 0)
            return new List<ResultadoBusca>();

        _logger.LogInformation(
            "[Normalizador] Iniciando normalizacao de {Quantidade} resultados",
            resultados.Count);

        // Etapa 1: Padronizar campos
        var padronizados = PadronizarCampos(resultados);

        // Etapa 2: Remover duplicatas (mesmo codigo de voo, fica o mais barato)
        var deduplicados = Deduplicar(padronizados);

        // Etapa 3: Remover outliers de preco
        var semOutliers = RemoverOutliers(deduplicados);

        // Etapa 4: Ordenar por preco total crescente
        var ordenados = semOutliers.OrderBy(v => v.PrecoTotal).ToList();

        _logger.LogInformation(
            "[Normalizador] Normalizacao concluida: {Quantidade} resultados apos processamento",
            ordenados.Count);

        return ordenados;
    }

    /// <summary>
    /// Padroniza campos de todos os resultados:
    /// - Companhia: Primeira letra maiuscula
    /// - CodigoVoo: Maiusculo
    /// - PrecoTotal, PrecoSemTaxas, Taxas: Valores positivos
    /// - Origem/Destino: Maiusculo
    /// - Fonte: minusculo
    /// </summary>
    private static List<ResultadoBusca> PadronizarCampos(List<ResultadoBusca> resultados)
    {
        foreach (var voo in resultados)
        {
            voo.CodigoVoo = (voo.CodigoVoo ?? string.Empty).ToUpperInvariant();
            voo.Companhia = FormatarNomeCompanhia(voo.Companhia);
            voo.Origem = (voo.Origem ?? string.Empty).ToUpperInvariant();
            voo.Destino = (voo.Destino ?? string.Empty).ToUpperInvariant();
            voo.Fonte = (voo.Fonte ?? string.Empty).ToLowerInvariant();
            voo.TipoTarifa = FormatarTipoTarifa(voo.TipoTarifa);

            // Garantir valores positivos
            voo.PrecoTotal = Math.Abs(voo.PrecoTotal);
            voo.PrecoSemTaxas = Math.Abs(voo.PrecoSemTaxas);
            voo.Taxas = Math.Abs(voo.Taxas);
            voo.DuracaoMinutos = Math.Max(30, voo.DuracaoMinutos); // Minimo 30 min
            voo.Paradas = Math.Max(0, voo.Paradas);
        }

        return resultados;
    }

    /// <summary>
    /// Remove duplicatas baseado no CodigoVoo + Companhia.
    /// Para voos com mesmo codigo e companhia, mantem o mais barato.
    /// </summary>
    private static List<ResultadoBusca> Deduplicar(List<ResultadoBusca> resultados)
    {
        var dedup = new Dictionary<string, ResultadoBusca>(StringComparer.OrdinalIgnoreCase);

        foreach (var voo in resultados)
        {
            var chave = $"{voo.CodigoVoo}|{voo.Companhia}";

            if (!dedup.ContainsKey(chave))
            {
                dedup[chave] = voo;
            }
            else if (voo.PrecoTotal < dedup[chave].PrecoTotal)
            {
                // Mantem o mais barato
                dedup[chave] = voo;
            }
            // Se mesmo preco, mantem o primeiro encontrado
        }

        return dedup.Values.ToList();
    }

    /// <summary>
    /// Remove outliers de preco usando o metodo do desvio padrao.
    /// Um preco e considerado outlier se estiver acima de
    /// (media + 3 * desvioPadrao) ou abaixo de (media - 3 * desvioPadrao).
    /// </summary>
    private static List<ResultadoBusca> RemoverOutliers(List<ResultadoBusca> resultados)
    {
        if (resultados.Count < 4)
            return resultados; // Poucos dados para identificar outliers

        var precos = resultados.Select(v => (double)v.PrecoTotal).ToList();
        var media = precos.Average();
        var somaQuadrados = precos.Sum(p => Math.Pow(p - media, 2));
        var desvioPadrao = Math.Sqrt(somaQuadrados / precos.Count);

        var limiteSuperior = media + (3 * desvioPadrao);
        var limiteInferior = Math.Max(0, media - (3 * desvioPadrao));

        return resultados
            .Where(v => v.PrecoTotal >= (decimal)limiteInferior &&
                        v.PrecoTotal <= (decimal)limiteSuperior)
            .ToList();
    }

    /// <summary>
    /// Formata o nome da companhia aerea (primeira letra maiuscula).
    /// </summary>
    private static string FormatarNomeCompanhia(string? nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return string.Empty;

        nome = nome.Trim().ToLowerInvariant();
        return nome switch
        {
            "latam" => "LATAM",
            "gol" => "GOL",
            "azul" => "AZUL",
            "decolar" => "Decolar",
            _ => char.ToUpper(nome[0]) + nome[1..]
        };
    }

    /// <summary>
    /// Formata o tipo de tarifa para padrao.
    /// </summary>
    private static string FormatarTipoTarifa(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            return "Economica";

        tipo = tipo.Trim().ToLowerInvariant();
        return tipo switch
        {
            "promo" or "promocional" => "Promo",
            "economica" or "economy" or "econômica" => "Economica",
            "executiva" or "business" => "Executiva",
            "primeira classe" or "first class" or "first" => "Primeira Classe",
            _ => "Economica"
        };
    }
}
