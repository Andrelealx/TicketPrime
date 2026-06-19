using Dapper;
using Microsoft.Data.Sqlite;
using RedCodeApi.Dtos.FlyCompare;
using RedCodeApi.Services.Scrapers;

namespace RedCodeApi.Services;

/// <summary>
/// Agendador de scraping que atualiza periodicamente o cache com precos
/// das rotas mais populares (SPEC-022) e verifica alertas de preco (SPEC-025).
///
/// Executado pelo Hangfire a cada 6 horas para pre-aquecer o cache,
/// garantindo que os usuarios encontrem dados rapidamente sem esperar
/// a execucao dos scrapers em tempo real.
/// A verificacao de alertas roda a cada 2 horas.
/// </summary>
public class ScrapingScheduler
{
    private readonly IEnumerable<IVooScraper> _scrapers;
    private readonly NormalizadorDados _normalizador;
    private readonly CacheService _cache;
    private readonly EmailService _emailService;
    private readonly ILogger<ScrapingScheduler> _logger;
    private readonly string _connStr;

    /// <summary>
    /// Rotas populares pre-definidas para pre-aquecimento de cache.
    /// Alinhado com o seed do DbInitializer (22 rotas).
    /// </summary>
    private static readonly (string Origem, string Destino)[] RotasPopulares =
    {
        ("GRU", "REC"), ("REC", "GRU"),  // SP-Recife
        ("GRU", "GIG"), ("GIG", "GRU"),  // SP-Rio (Galeao)
        ("CGH", "SDU"), ("SDU", "CGH"),  // Congonhas-Santos Dumont (Ponte Aerea)
        ("GRU", "BSB"), ("BSB", "GRU"),  // SP-Brasilia
        ("GRU", "SSA"), ("SSA", "GRU"),  // SP-Salvador
        ("GRU", "FOR"), ("FOR", "GRU"),  // SP-Fortaleza
        ("GRU", "CNF"), ("CNF", "GRU"),  // SP-Belo Horizonte
        ("CGH", "POA"), ("POA", "CGH"),  // SP-Porto Alegre
        ("CGH", "CWB"), ("CWB", "CGH"),  // SP-Curitiba
        ("GRU", "VIX"), ("VIX", "GRU"),  // SP-Vitoria
        ("CGH", "FLN"), ("FLN", "CGH")   // SP-Florianopolis
    };

    public ScrapingScheduler(
        IEnumerable<IVooScraper> scrapers,
        NormalizadorDados normalizador,
        CacheService cache,
        EmailService emailService,
        ILogger<ScrapingScheduler> logger,
        string connStr)
    {
        _scrapers = scrapers;
        _normalizador = normalizador;
        _cache = cache;
        _emailService = emailService;
        _logger = logger;
        _connStr = connStr;
    }

    /// <summary>
    /// Atualiza o cache para todas as rotas populares.
    /// Chamado pelo Hangfire a cada 6 horas.
    /// </summary>
    public async Task AtualizarRotasPopulares()
    {
        _logger.LogInformation(
            "[ScrapingScheduler] Iniciando atualizacao de cache para {Quantidade} rotas populares",
            RotasPopulares.Length);

        int sucesso = 0;
        int falhas = 0;

        foreach (var (origem, destino) in RotasPopulares)
        {
            try
            {
                var data = DateTime.Today.AddDays(1); // Amanha

                _logger.LogDebug(
                    "[ScrapingScheduler] Atualizando rota {Origem}-{Destino}...",
                    origem, destino);

                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                var tasks = _scrapers.Select(s =>
                    s.BuscarVoosAsync(origem, destino, data, cts.Token));
                var resultados = (await Task.WhenAll(tasks))
                    .SelectMany(r => r)
                    .ToList();

                if (resultados.Count > 0)
                {
                    resultados = _normalizador.Normalizar(resultados);
                    await _cache.ArmazenarAsync(origem, destino, data, resultados);
                    _logger.LogInformation(
                        "[ScrapingScheduler] Cache atualizado: {Origem}-{Destino} ({Qtd} voos)",
                        origem, destino, resultados.Count);
                    sucesso++;
                }
                else
                {
                    _logger.LogWarning(
                        "[ScrapingScheduler] Nenhum resultado para {Origem}-{Destino}",
                        origem, destino);
                    falhas++;
                }

                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[ScrapingScheduler] Erro ao atualizar rota {Origem}-{Destino}",
                    origem, destino);
                falhas++;
            }
        }

        _logger.LogInformation(
            "[ScrapingScheduler] Atualizacao concluida: {Sucesso} sucesso, {Falhas} falhas",
            sucesso, falhas);
    }

    /// <summary>
    /// Verifica alertas de preco ativos e dispara notificacao quando o menor preco
    /// das ultimas 6 horas esta abaixo do preco alvo (SPEC-025).
    /// Chamado pelo Hangfire a cada 2 horas.
    /// </summary>
    public async Task VerificarAlertas()
    {
        _logger.LogInformation(
            "[ScrapingScheduler] Iniciando verificacao de alertas de preco...");

        using var db = new SqliteConnection(_connStr);

        // Buscar alertas ativos com seus menores precos atuais
        var alertasComPrecos = await db.QueryAsync<AlertaComPreco>(
            @"SELECT a.Id, a.Email, a.PrecoAlvo, a.RotaId,
                     a1.CodigoIATA AS Origem,
                     a2.CodigoIATA AS Destino,
                     (SELECT MIN(p.PrecoTotal)
                      FROM Precos p
                      INNER JOIN Voos v ON p.VooId = v.Id
                      WHERE v.RotaId = a.RotaId
                        AND p.DataColeta > datetime('now', '-6 hours')
                     ) AS MenorPrecoAtual
              FROM AlertasPreco a
              INNER JOIN Rotas r ON a.RotaId = r.Id
              INNER JOIN Aeroportos a1 ON r.OrigemId = a1.Id
              INNER JOIN Aeroportos a2 ON r.DestinoId = a2.Id
              WHERE a.Ativo = 1
                AND MenorPrecoAtual IS NOT NULL
                AND MenorPrecoAtual <= a.PrecoAlvo");

        int disparados = 0;

        foreach (var alerta in alertasComPrecos)
        {
            _logger.LogWarning(
                "🔥 [ALERTA-DISPARADO] {Email}: {Origem} -> {Destino} por R$ {Preco} (alvo era R$ {Alvo})",
                alerta.Email, alerta.Origem, alerta.Destino, alerta.MenorPrecoAtual, alerta.PrecoAlvo);

            // Envia notificacao por email (LOW-01)
            await _emailService.EnviarNotificacaoAlertaAsync(
                alerta.Email,
                alerta.Origem,
                alerta.Destino,
                alerta.MenorPrecoAtual,
                alerta.PrecoAlvo);

            await db.ExecuteAsync(
                "UPDATE AlertasPreco SET Ativo = 0 WHERE Id = @Id",
                new { Id = alerta.Id });

            disparados++;
        }

        _logger.LogInformation(
            "[ScrapingScheduler] Verificacao de alertas concluida: {Disparados} alertas disparados",
            disparados);
    }

    private class AlertaComPreco
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public decimal PrecoAlvo { get; set; }
        public int RotaId { get; set; }
        public string Origem { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public decimal MenorPrecoAtual { get; set; }
    }
}
