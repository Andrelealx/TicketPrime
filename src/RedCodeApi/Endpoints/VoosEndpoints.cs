using Dapper;
using Microsoft.Data.Sqlite;
using RedCodeApi.Data;
using RedCodeApi.Dtos.FlyCompare;
using RedCodeApi.Models.FlyCompare;
using RedCodeApi.Services;
using RedCodeApi.Services.Scrapers;

namespace RedCodeApi.Endpoints;

public static class VoosEndpoints
{
    public static void MapVoosEndpoints(this WebApplication app, string connectionString)
    {
        app.MapGet("/api/voos/busca", async (
            string origem,
            string destino,
            DateTime dataPartida,
            CacheService cache,
            IEnumerable<IVooScraper> scrapers,
            NormalizadorDados normalizador,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(origem) || origem.Length != 3)
                return Results.BadRequest("Erro: Codigo IATA de origem invalido.");
            if (string.IsNullOrWhiteSpace(destino) || destino.Length != 3)
                return Results.BadRequest("Erro: Codigo IATA de destino invalido.");
            if (dataPartida < DateTime.Today)
                return Results.BadRequest("Erro: Data de partida nao pode ser no passado.");

            var origemUpper = origem.ToUpper();
            var destinoUpper = destino.ToUpper();

            // 1. Cache
            var cacheResultados = await cache.ObterAsync(origemUpper, destinoUpper, dataPartida);
            if (cacheResultados is { Count: > 0 })
            {
                logger.LogInformation(
                    "[Busca] Cache HIT para {Origem}-{Destino} em {Data}: {Qtd} resultados",
                    origemUpper, destinoUpper, dataPartida.ToString("yyyy-MM-dd"), cacheResultados.Count);
                return Results.Ok(cacheResultados);
            }

            logger.LogInformation(
                "[Busca] Cache MISS para {Origem}-{Destino} em {Data}. Iniciando scraping...",
                origemUpper, destinoUpper, dataPartida.ToString("yyyy-MM-dd"));

            // 2. Scrapers em paralelo
            var tarefasScrapers = scrapers.Select(s =>
                s.BuscarVoosAsync(origemUpper, destinoUpper, dataPartida, cancellationToken));
            var resultadosScrapers = await Task.WhenAll(tarefasScrapers);

            // 3. Agregar
            var todosResultados = resultadosScrapers.SelectMany(r => r).ToList();

            // 4. Fallback mock (se nenhum scraper retornou dados)
            if (todosResultados.Count == 0)
            {
                logger.LogWarning(
                    "[Busca] Nenhum scraper retornou dados para {Origem}-{Destino}. Usando mock fallback.",
                    origemUpper, destinoUpper);
                todosResultados = MockVoosGenerator.Gerar(origemUpper, destinoUpper, dataPartida);
            }

            // 5. Normalizar
            var normalizados = normalizador.Normalizar(todosResultados);

            if (normalizados.Count == 0)
            {
                logger.LogWarning(
                    "[Busca] Nenhum resultado apos normalizacao para {Origem}-{Destino}.",
                    origemUpper, destinoUpper);
                return Results.Ok(new List<ResultadoBusca>());
            }

            // 6. Cache
            await cache.ArmazenarAsync(origemUpper, destinoUpper, dataPartida, normalizados);

            // 7. Persistir historico de precos em background (LOW-06)
            var persistir = normalizados.Select(v => new VooParaPersistir
            {
                CodigoVoo = v.CodigoVoo,
                Companhia = v.Companhia,
                PrecoSemTaxas = v.PrecoSemTaxas,
                Taxas = v.Taxas,
                PrecoTotal = v.PrecoTotal,
                TipoTarifa = v.TipoTarifa,
                Bagagem = v.BagagemIncluida ? 1 : 0,
                UrlCompra = v.UrlCompra,
                Fonte = v.Fonte,
                Paradas = v.Paradas,
                DuracaoMinutos = v.DuracaoMinutos,
                Partida = v.Partida.ToString("yyyy-MM-dd HH:mm:ss"),
                Chegada = v.Chegada.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();

            _ = Task.Run(async () =>
            {
                try
                {
                    using var db = new SqliteConnection(connectionString);
                    await PersistirHistoricoPrecosAsync(db, origemUpper, destinoUpper, persistir, logger);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "[Busca] Erro ao persistir historico de precos para {Origem}-{Destino}",
                        origemUpper, destinoUpper);
                }
            });

            // 8. Retornar
            return Results.Ok(normalizados);
        });

        app.MapGet("/api/voos/precos/{vooId}", async (int vooId) =>
        {
            using var db = new SqliteConnection(connectionString);

            var voo = await db.QueryFirstOrDefaultAsync<Voo>(
                "SELECT * FROM Voos WHERE Id = @Id", new { Id = vooId });

            if (voo == null)
                return Results.NotFound("Voo nao encontrado.");

            var companhia = await db.QueryFirstOrDefaultAsync<CompanhiaAerea>(
                "SELECT * FROM CompanhiasAereas WHERE Id = @Id", new { Id = voo.CompanhiaId });

            var precos = await db.QueryAsync<PrecoVoo>(
                @"SELECT * FROM Precos
                  WHERE VooId = @VooId
                  ORDER BY DataColeta DESC",
                new { VooId = vooId });

            return Results.Ok(new PrecoHistoricoResponse
            {
                CodigoVoo = voo.CodigoVoo,
                Companhia = companhia?.Nome ?? string.Empty,
                Precos = precos.Select(p => new PrecoHistoricoPonto
                {
                    Preco = p.PrecoTotal,
                    DataColeta = p.DataColeta,
                    Fonte = p.Fonte
                }).ToList()
            });
        });
    }

    /// <summary>
    /// DTO privado para transportar dados de voo a serem persistidos (LOW-06).
    /// </summary>
    private sealed class VooParaPersistir
    {
        public string CodigoVoo { get; set; } = string.Empty;
        public string Companhia { get; set; } = string.Empty;
        public decimal PrecoSemTaxas { get; set; }
        public decimal Taxas { get; set; }
        public decimal PrecoTotal { get; set; }
        public string TipoTarifa { get; set; } = string.Empty;
        public int Bagagem { get; set; }
        public string UrlCompra { get; set; } = string.Empty;
        public string Fonte { get; set; } = string.Empty;
        public int Paradas { get; set; }
        public int DuracaoMinutos { get; set; }
        public string Partida { get; set; } = string.Empty;
        public string Chegada { get; set; } = string.Empty;
    }

    /// <summary>
    /// Persiste o historico de precos dos voos no banco de dados (SQLite).
    /// Extraido para metodo separado para facilitar testes e reuso (LOW-06).
    /// </summary>
    private static async Task PersistirHistoricoPrecosAsync(
        SqliteConnection db,
        string origem,
        string destino,
        List<VooParaPersistir> voosParaPersistir,
        ILogger logger)
    {
        var rotaId = await db.QueryFirstOrDefaultAsync<int?>(
            @"SELECT r.Id FROM Rotas r
              INNER JOIN Aeroportos a1 ON r.OrigemId = a1.Id
              INNER JOIN Aeroportos a2 ON r.DestinoId = a2.Id
              WHERE a1.CodigoIATA = @Origem AND a2.CodigoIATA = @Destino",
            new { Origem = origem, Destino = destino });

        if (rotaId == null)
        {
            logger.LogWarning(
                "[Busca] Rota {Origem}-{Destino} nao encontrada no banco. Historico nao sera salvo.",
                origem, destino);
            return;
        }

        foreach (var voo in voosParaPersistir)
        {
            var ciaId = await db.QueryFirstOrDefaultAsync<int?>(
                "SELECT Id FROM CompanhiasAereas WHERE Codigo = @Codigo",
                new { Codigo = voo.Companhia });

            if (ciaId == null) continue;

            var vooId = await db.QueryFirstOrDefaultAsync<int?>(
                @"SELECT Id FROM Voos WHERE CodigoVoo = @CodigoVoo AND CompanhiaId = @CiaId AND RotaId = @RotaId AND DataPartida = @DataPartida",
                new { voo.CodigoVoo, CiaId = ciaId, RotaId = rotaId, voo.Partida });

            if (vooId == null)
            {
                vooId = await db.QuerySingleAsync<int>(
                    @"INSERT INTO Voos (RotaId, CompanhiaId, CodigoVoo, DataPartida, DataChegada, DuracaoMinutos, Paradas)
                      VALUES (@RotaId, @CiaId, @CodigoVoo, @DataPartida, @DataChegada, @Duracao, @Paradas);
                      SELECT last_insert_rowid();",
                    new
                    {
                        RotaId = rotaId,
                        CiaId = ciaId,
                        voo.CodigoVoo,
                        DataPartida = voo.Partida,
                        DataChegada = voo.Chegada,
                        Duracao = voo.DuracaoMinutos,
                        voo.Paradas
                    });
            }

            await db.ExecuteAsync(
                @"INSERT INTO Precos (VooId, Preco, Taxas, PrecoTotal, Moeda, TipoTarifa, BagagemIncluida, UrlDestino, Fonte, DataColeta)
                  VALUES (@VooId, @Preco, @Taxas, @PrecoTotal, @Moeda, @TipoTarifa, @Bagagem, @Url, @Fonte, datetime('now'))",
                new
                {
                    VooId = vooId,
                    Preco = voo.PrecoSemTaxas,
                    voo.Taxas,
                    voo.PrecoTotal,
                    Moeda = "BRL",
                    voo.TipoTarifa,
                    voo.Bagagem,
                    Url = voo.UrlCompra,
                    voo.Fonte
                });
        }

        logger.LogInformation(
            "[Busca] Historico salvo: {Qtd} voos persistidos para {Origem}-{Destino}",
            voosParaPersistir.Count, origem, destino);
    }
}
