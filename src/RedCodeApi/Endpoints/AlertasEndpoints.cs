using Dapper;
using Microsoft.Data.Sqlite;
using RedCodeApi.Dtos.FlyCompare;
using RedCodeApi.Models.FlyCompare;

namespace RedCodeApi.Endpoints;

public static class AlertasEndpoints
{
    public static void MapAlertasEndpoints(this WebApplication app, string connectionString)
    {
        // POST /api/alertas
        app.MapPost("/api/alertas", async (AlertaRequest req) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
                return Results.BadRequest("Erro: E-mail inválido.");
            if (string.IsNullOrWhiteSpace(req.Origem) || req.Origem.Length != 3)
                return Results.BadRequest("Erro: Código IATA de origem inválido.");
            if (string.IsNullOrWhiteSpace(req.Destino) || req.Destino.Length != 3)
                return Results.BadRequest("Erro: Código IATA de destino inválido.");
            if (req.PrecoAlvo <= 0)
                return Results.BadRequest("Erro: Preço alvo deve ser maior que zero.");

            using var db = new SqliteConnection(connectionString);

            var rota = await db.QueryFirstOrDefaultAsync<Rota>(
                @"SELECT r.* FROM Rotas r
                  INNER JOIN Aeroportos a1 ON r.OrigemId = a1.Id
                  INNER JOIN Aeroportos a2 ON r.DestinoId = a2.Id
                  WHERE a1.CodigoIATA = @Origem AND a2.CodigoIATA = @Destino",
                new { Origem = req.Origem.ToUpper(), Destino = req.Destino.ToUpper() });

            if (rota == null)
                return Results.NotFound("Rota não encontrada. Verifique os aeroportos.");

            var alerta = new AlertaPreco
            {
                Email = req.Email.ToLower().Trim(),
                RotaId = rota.Id,
                PrecoAlvo = req.PrecoAlvo
            };

            await db.ExecuteAsync(
                @"INSERT INTO AlertasPreco (Email, RotaId, PrecoAlvo)
                  VALUES (@Email, @RotaId, @PrecoAlvo)",
                alerta);

            return Results.Created($"/api/alertas/{alerta.Email}", new
            {
                Mensagem = "Alerta criado com sucesso!",
                Rota = $"{req.Origem.ToUpper()} → {req.Destino.ToUpper()}",
                PrecoAlvo = req.PrecoAlvo
            });
        });

        // GET /api/alertas/{email}
        app.MapGet("/api/alertas/{email}", async (string email) =>
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return Results.BadRequest("Erro: E-mail inválido.");

            using var db = new SqliteConnection(connectionString);

            var alertas = await db.QueryAsync(
                @"SELECT a.Id, a.Email, a.PrecoAlvo, a.Ativo, a.DataCriacao,
                         a1.CodigoIATA AS Origem, a1.Cidade AS OrigemCidade,
                         a2.CodigoIATA AS Destino, a2.Cidade AS DestinoCidade
                  FROM AlertasPreco a
                  INNER JOIN Rotas r ON a.RotaId = r.Id
                  INNER JOIN Aeroportos a1 ON r.OrigemId = a1.Id
                  INNER JOIN Aeroportos a2 ON r.DestinoId = a2.Id
                  WHERE a.Email = @Email
                  ORDER BY a.DataCriacao DESC",
                new { Email = email.ToLower().Trim() });

            return Results.Ok(alertas);
        });
    }
}
