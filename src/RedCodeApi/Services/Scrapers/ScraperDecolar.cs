using PuppeteerSharp;
using RedCodeApi.Dtos.FlyCompare;

namespace RedCodeApi.Services.Scrapers;

/// <summary>
/// Scraper para a Decolar (OTA - Online Travel Agency).
/// Usa PuppeteerSharp (browser headless) para renderizar JavaScript pesado.
/// </summary>
public class ScraperDecolar : IVooScraper
{
    private readonly ILogger<ScraperDecolar> _logger;
    private static readonly SemaphoreSlim _browserLock = new(1, 1);
    private static IBrowser? _browserCompartilhado;
    private static DateTime _ultimoHealthCheck = DateTime.MinValue;
    private static readonly TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan _browserMaxIdleTime = TimeSpan.FromMinutes(30);

    public string Nome => "decolar";
    public int Ordem => 4; // Executado por ultimo (mais lento)

    public ScraperDecolar(ILogger<ScraperDecolar> logger)
    {
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
            _logger.LogInformation(
                "[ScraperDecolar] Iniciando busca headless: {Origem}-{Destino} em {Data}",
                origem, destino, dataPartida.ToString("yyyy-MM-dd"));

            var browser = await ObterBrowserAsync();
            await using var page = await browser.NewPageAsync();

            await page.SetUserAgentAsync(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            // Configurar timeout da pagina
            page.DefaultTimeout = 30000; // 30 segundos

            var url = MontarUrlBusca(origem, destino, dataPartida);
            _logger.LogInformation("[ScraperDecolar] Navegando para: {Url}", url);

            await page.GoToAsync(url, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle2 },
                Timeout = 30000
            });

            // Aguardar resultados carregarem
            try
            {
                await page.WaitForSelectorAsync("[data-testid='flight-card']", new WaitForSelectorOptions
                {
                    Timeout = 25000
                });
            }
            catch (WaitTaskTimeoutException)
            {
                _logger.LogWarning(
                    "[ScraperDecolar] Timeout ao aguardar flight-card. Tentando fallback...");
                // Continua mesmo sem encontrar o seletor exato
            }

            // Extrair dados via JavaScript evaluation
            resultados = await ExtrairVoosViaJs(page, origem, destino, dataPartida);

            _logger.LogInformation(
                "[ScraperDecolar] Busca concluida: {Quantidade} voos encontrados",
                resultados.Count);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("[ScraperDecolar] Requisicao cancelada/timeout para {Origem}-{Destino}", origem, destino);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ScraperDecolar] Erro ao buscar voos {Origem}-{Destino}", origem, destino);
        }

        return resultados;
    }

    private static string MontarUrlBusca(string origem, string destino, DateTime dataPartida)
    {
        return $"https://www.decolar.com/passagens-aereas/{origem}+{destino}/{dataPartida:yyyy-MM-dd}";
    }

    private async Task<List<ResultadoBusca>> ExtrairVoosViaJs(
        IPage page,
        string origem,
        string destino,
        DateTime dataPartida)
    {
        var resultados = new List<ResultadoBusca>();

        try
        {
            var voosJson = await page.EvaluateFunctionAsync<string>(@"
                () => {
                    const cards = document.querySelectorAll('[data-testid=""flight-card""]');

                    if (cards.length === 0) {
                        // Fallback: tenta seletores alternativos
                        const altCards = document.querySelectorAll(
                            '[class*=""flight""], [class*=""card""], [class*=""resultado""]');
                        return JSON.stringify(Array.from(altCards).slice(0, 20).map(card => ({
                            codigo: (card.querySelector('[class*=""code""], [class*=""flight-number""]')?.innerText || '').trim(),
                            preco: parseFloat((card.innerText.match(/R?\$?\s*([0-9]+[.,][0-9]{2})/) || [,'0'])[1].replace(',', '.')),
                            duracao: (() => {
                                const match = card.innerText.match(/(\d+)\s*h\s*(\d+)?\s*m/);
                                if (match) return parseInt(match[1]) * 60 + parseInt(match[2] || '0');
                                return 0;
                            })(),
                            companhia: (card.querySelector('[class*=""airline""], [class*""company""]')?.innerText || 'Decolar').trim()
                        })));
                    }

                    return JSON.stringify(Array.from(cards).slice(0, 20).map(card => ({
                        codigo: (card.querySelector('[data-testid=""flight-code""]')?.innerText || '').trim(),
                        preco: parseFloat((card.querySelector('[data-testid=""price""]')?.innerText || '0').replace(/[^0-9,]/g,'').replace(',','.')),
                        duracao: (() => {
                            const durText = card.querySelector('[data-testid=""duration""]')?.innerText || '';
                            const match = durText.match(/(\d+)\s*h\s*(\d+)?\s*m/);
                            if (match) return parseInt(match[1]) * 60 + parseInt(match[2] || '0');
                            return 0;
                        })(),
                        companhia: (card.querySelector('[data-testid=""airline""]')?.innerText || 'Decolar').trim()
                    })));
                }");

            if (string.IsNullOrWhiteSpace(voosJson) || voosJson == "[]")
                return resultados;

            var voosData = System.Text.Json.JsonSerializer.Deserialize<List<VooDecolarJson>>(voosJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (voosData == null)
                return resultados;

            int index = 0;
            foreach (var v in voosData)
            {
                if (string.IsNullOrWhiteSpace(v.Codigo) || v.Preco <= 0)
                    continue;

                var duracao = v.Duracao > 0 ? v.Duracao : 200; // default ~3h20
                var taxas = Math.Round(v.Preco * 0.1m, 2);

                resultados.Add(new ResultadoBusca
                {
                    CodigoVoo = v.Codigo.ToUpperInvariant(),
                    Companhia = string.IsNullOrWhiteSpace(v.Companhia) ? "Decolar" : v.Companhia,
                    Origem = origem.ToUpper(),
                    Destino = destino.ToUpper(),
                    Partida = dataPartida.AddHours(6 + index * 2),
                    Chegada = dataPartida.AddHours(6 + index * 2).AddMinutes(duracao),
                    DuracaoMinutos = duracao,
                    Paradas = 0,
                    PrecoTotal = v.Preco,
                    PrecoSemTaxas = Math.Round(v.Preco - taxas, 2),
                    Taxas = taxas,
                    TipoTarifa = "Economica",
                    BagagemIncluida = false,
                    UrlCompra = MontarUrlBusca(origem, destino, dataPartida),
                    Fonte = "decolar"
                });

                index++;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ScraperDecolar] Erro ao extrair dados via JavaScript");
        }

        return resultados;
    }

    private async Task<IBrowser> ObterBrowserAsync()
    {
        // Health check: verifica se o browser ainda esta saudavel (LOW-02)
        if (_browserCompartilhado != null && !_browserCompartilhado.IsClosed)
        {
            var precisaHealthCheck = DateTime.UtcNow - _ultimoHealthCheck > _healthCheckInterval;
            if (precisaHealthCheck)
            {
                try
                {
                    var pages = await _browserCompartilhado.PagesAsync();
                    _ultimoHealthCheck = DateTime.UtcNow;
                    _logger.LogDebug("[ScraperDecolar] Health check OK: browser ativo com {Paginas} paginas", pages.Length);
                    return _browserCompartilhado;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[ScraperDecolar] Health check FALHOU. Reiniciando browser...");
                    try { await _browserCompartilhado.CloseAsync(); } catch { /* best effort */ }
                    _browserCompartilhado = null;
                }
            }
            else
            {
                return _browserCompartilhado;
            }
        }

        await _browserLock.WaitAsync();
        try
        {
            // Double-check apos adquirir lock
            if (_browserCompartilhado != null && !_browserCompartilhado.IsClosed)
            {
                _ultimoHealthCheck = DateTime.UtcNow;
                return _browserCompartilhado;
            }

            // Baixar browser (se nao existir)
            _logger.LogInformation("[ScraperDecolar] Baixando Chromium...");
            await new BrowserFetcher().DownloadAsync();

            _logger.LogInformation("[ScraperDecolar] Iniciando browser headless...");
            _browserCompartilhado = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-gpu"
                }
            });

            _ultimoHealthCheck = DateTime.UtcNow;
            return _browserCompartilhado;
        }
        finally
        {
            _browserLock.Release();
        }
    }

    /// <summary>
    /// Classe auxiliar para deserializar dados extraidos via JavaScript.
    /// </summary>
    private class VooDecolarJson
    {
        public string Codigo { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public int Duracao { get; set; }
        public string Companhia { get; set; } = string.Empty;
    }
}
