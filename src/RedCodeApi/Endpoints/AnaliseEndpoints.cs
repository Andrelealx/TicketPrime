using RedCodeApi.Dtos.FlyCompare;
using RedCodeApi.Services;

namespace RedCodeApi.Endpoints;

/// <summary>
/// Endpoint de análise de preços (SPEC-034 — Motor de Regras + Score).
/// 
/// Fornece análise inteligente de preços usando o AnalisadorPrecosService:
/// - Score de 1 a 5 estrelas por voo
/// - Comparação com histórico de preços da rota
/// - Fatores detalhados (preço, antecedência, competitividade, benefícios)
/// - Justificativa textual em português
/// </summary>
public static class AnaliseEndpoints
{
    public static void MapAnaliseEndpoints(this WebApplication app, string connectionString)
    {
        // POST /api/voos/analise — analisa uma lista de resultados de busca
        app.MapPost("/api/voos/analise", async (
            AnaliseRequest req,
            AnalisadorPrecosService analisador) =>
        {
            if (req.Resultados == null || req.Resultados.Count == 0)
                return Results.BadRequest("Erro: Lista de resultados vazia.");

            if (!DateTime.TryParse(req.DataPartida, out var dataPartida))
                return Results.BadRequest("Erro: Data de partida inválida. Use formato yyyy-MM-dd.");

            var analises = analisador.Analisar(req.Resultados, dataPartida);
            return Results.Ok(analises);
        });

        // GET /api/voos/analise — versão simplificada que recebe parâmetros de busca
        // e delega ao endpoint de busca + analisador
        app.MapGet("/api/voos/analise/resumo", async (
            string origem,
            string destino,
            string dataPartida,
            AnalisadorPrecosService analisador,
            ILogger<Program> logger) =>
        {
            if (string.IsNullOrWhiteSpace(origem) || origem.Length != 3)
                return Results.BadRequest("Erro: Código IATA de origem inválido.");
            if (string.IsNullOrWhiteSpace(destino) || destino.Length != 3)
                return Results.BadRequest("Erro: Código IATA de destino inválido.");
            if (!DateTime.TryParse(dataPartida, out var data))
                return Results.BadRequest("Erro: Data de partida inválida. Use formato yyyy-MM-dd.");

            // Redireciona — o frontend chama este endpoint para obter só o resumo
            // das métricas históricas da rota (sem precisar enviar todos os resultados)
            logger.LogInformation(
                "[Analise/Resumo] Obtendo métricas históricas para {Origem}-{Destino}",
                origem.ToUpper(), destino.ToUpper());

            // Cria um resultado dummy só para obter as estatísticas da rota
            var dummy = new List<ResultadoBusca>
            {
                new()
                {
                    Origem = origem.ToUpper(),
                    Destino = destino.ToUpper(),
                    CodigoVoo = "ANALISE",
                    Companhia = "Resumo",
                    PrecoTotal = 0,
                    Partida = data
                }
            };

            var analises = analisador.Analisar(dummy, data);
            var resumo = analises.FirstOrDefault();

            return resumo != null
                ? Results.Ok(new
                {
                    resumo.PrecoMedioHistorico,
                    resumo.MenorPrecoHistorico,
                    resumo.DiasAtePartida,
                    Mensagem = resumo.DiasAtePartida switch
                    {
                        >= 30 => $"Faltam {resumo.DiasAtePartida} dias — boa antecedência!",
                        >= 14 => $"Faltam {resumo.DiasAtePartida} dias — considere comprar em breve.",
                        >= 7 => $"Faltam apenas {resumo.DiasAtePartida} dias — preços podem subir.",
                        _ => $"Urgente: faltam só {resumo.DiasAtePartida} dias."
                    }
                })
                : Results.Ok(new { Mensagem = "Sem dados históricos suficientes para esta rota." });
        });
    }
}

/// <summary>
/// Request para o endpoint de análise de preços.
/// </summary>
public class AnaliseRequest
{
    public List<ResultadoBusca> Resultados { get; set; } = new();
    public string DataPartida { get; set; } = string.Empty;
}
