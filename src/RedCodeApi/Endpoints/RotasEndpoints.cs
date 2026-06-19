using Dapper;
using Microsoft.Data.Sqlite;
using RedCodeApi.Models.FlyCompare;

namespace RedCodeApi.Endpoints;

public static class RotasEndpoints
{
    public static void MapRotasEndpoints(this WebApplication app, string connectionString)
    {
        app.MapGet("/api/rotas/populares", async () =>
        {
            using var db = new SqliteConnection(connectionString);
            var rotas = await db.QueryAsync<Rota>(
                @"SELECT r.Id, r.OrigemId, r.DestinoId,
                         a1.CodigoIATA AS OrigemCodigo, a1.Cidade AS OrigemCidade,
                         a2.CodigoIATA AS DestinoCodigo, a2.Cidade AS DestinoCidade
                  FROM Rotas r
                  INNER JOIN Aeroportos a1 ON r.OrigemId = a1.Id
                  INNER JOIN Aeroportos a2 ON r.DestinoId = a2.Id
                  ORDER BY a1.Cidade, a2.Cidade");
            return Results.Ok(rotas);
        });
    }
}
