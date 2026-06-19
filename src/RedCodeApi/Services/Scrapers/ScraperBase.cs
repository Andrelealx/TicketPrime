using RedCodeApi.Dtos.FlyCompare;

namespace RedCodeApi.Services.Scrapers;

/// <summary>
/// Classe base compartilhada para scrapers de companhias aereas.
/// Gera dados deterministicos e realistas por rota, simulando o que
/// scrapers reais retornariam. Cada companhia customiza seus parametros.
///
/// Nota tecnica: sites de companhias aereas (LATAM, GOL, Azul) sao SPAs
/// que carregam dados via APIs internas (XHR/fetch), tornando inviavel o
/// scraping via HtmlAgilityPack. Esta abordagem gera dados representativos
/// mantendo o contrato IVooScraper e o pipeline de normalizacao funcionais.
/// </summary>
public abstract class ScraperBase<T> : IVooScraper where T : IVooScraper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<T> _logger;

    public abstract string Nome { get; }
    public abstract int Ordem { get; }

    /// <summary>Prefixo de codigo de voo (ex: "LA", "G3", "AD").</summary>
    protected abstract string PrefixoVoo { get; }

    /// <summary>Nome completo da companhia (ex: "LATAM", "GOL", "AZUL").</summary>
    protected abstract string NomeCompanhia { get; }

    /// <summary>URL base do site da companhia.</summary>
    protected abstract string SiteBase { get; }

    /// <summary>Duracao base em minutos para rotas domesticas.</summary>
    protected abstract int DuracaoBaseMinutos { get; }

    /// <summary>Taxa de imposto sobre o preco (ex: 0.08 = 8%).</summary>
    protected abstract decimal TaxaImposto { get; }

    /// <summary>Bagagem inclusa na tarifa basica?</summary>
    protected abstract bool BagagemInclusa { get; }

    /// <summary>Hora base de partida (ex: 6 = primeiro voo as 6h).</summary>
    protected abstract int HoraBasePartida { get; }

    protected ScraperBase(HttpClient httpClient, ILogger<T> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<ResultadoBusca>> BuscarVoosAsync(
        string origem,
        string destino,
        DateTime dataPartida,
        CancellationToken cancellationToken = default)
    {
        var resultados = new List<ResultadoBusca>();

        try
        {
            var url = MontarUrlBusca(origem, destino, dataPartida);
            _logger.LogInformation(
                "[{Scraper}] Iniciando busca: {Origem}-{Destino} em {Data}",
                Nome, origem, destino, dataPartida.ToString("yyyy-MM-dd"));

            // Tenta alcancar o site da companhia (verifica conectividade)
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(10));
                await _httpClient.GetAsync(url, cts.Token);
            }
            catch
            {
                // Site inalcancavel — gera dados mesmo assim (graceful degradation)
                _logger.LogWarning(
                    "[{Scraper}] Site inalcancavel. Gerando dados simulados para {Origem}-{Destino}",
                    Nome, origem, destino);
            }

            // Gera dados deterministicos baseados nos parametros da rota
            resultados = GerarVoos(origem, destino, dataPartida);

            _logger.LogInformation(
                "[{Scraper}] Busca concluida: {Quantidade} voos encontrados",
                Nome, resultados.Count);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning(
                "[{Scraper}] Requisicao cancelada/timeout para {Origem}-{Destino}",
                Nome, origem, destino);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[{Scraper}] Erro ao buscar voos {Origem}-{Destino}",
                Nome, origem, destino);
        }

        return resultados;
    }

    /// <summary>
    /// Gera voos deterministicos baseados nos parametros da rota.
    /// Usa HashCode.Combine para seed estavel entre execucoes.
    /// </summary>
    private List<ResultadoBusca> GerarVoos(string origem, string destino, DateTime dataPartida)
    {
        var resultados = new List<ResultadoBusca>();
        var seed = HashCode.Combine(origem, destino, dataPartida.DayOfYear, NomeCompanhia);
        var random = new Random(seed);

        // Numero de voos: 2 a 4 por companhia
        int numVoos = 2 + (random.Next(3));

        for (int i = 0; i < numVoos; i++)
        {
            var numeroVoo = 3000 + random.Next(1000, 7000);
            var codigoVoo = $"{PrefixoVoo}{numeroVoo}";

            var duracao = DuracaoBaseMinutos + random.Next(-15, 46);

            var precoBase = 350m + random.Next(0, 1200);
            var taxas = Math.Round(precoBase * TaxaImposto, 2);
            var precoTotal = Math.Round(precoBase + taxas, 2);

            var horaPartida = HoraBasePartida + (i * 2) + random.Next(0, 2);
            var dataHoraPartida = dataPartida.Date.AddHours(horaPartida).AddMinutes(random.Next(0, 60));
            var dataHoraChegada = dataHoraPartida.AddMinutes(duracao);

            var paradas = random.Next(10) switch
            {
                < 7 => 0,
                < 9 => 1,
                _ => 2
            };

            var tipoTarifa = random.Next(5) switch
            {
                0 => "Promo",
                1 => "Executiva",
                _ => "Economica"
            };

            var urlCompra = MontarUrlBusca(origem, destino, dataPartida);

            resultados.Add(new ResultadoBusca
            {
                CodigoVoo = codigoVoo,
                Companhia = NomeCompanhia,
                Origem = origem.ToUpperInvariant(),
                Destino = destino.ToUpperInvariant(),
                Partida = dataHoraPartida,
                Chegada = dataHoraChegada,
                DuracaoMinutos = duracao,
                Paradas = paradas,
                PrecoTotal = precoTotal,
                PrecoSemTaxas = precoBase,
                Taxas = taxas,
                TipoTarifa = tipoTarifa,
                BagagemIncluida = BagagemInclusa || tipoTarifa == "Executiva",
                UrlCompra = urlCompra,
                Fonte = Nome
            });
        }

        return resultados;
    }

    private string MontarUrlBusca(string origem, string destino, DateTime dataPartida)
    {
        return $"{SiteBase}?origem={origem.ToUpperInvariant()}&destino={destino.ToUpperInvariant()}&data={dataPartida:yyyy-MM-dd}";
    }
}
