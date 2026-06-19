using Dapper;
using Microsoft.Data.Sqlite;
using RedCodeApi.Models.FlyCompare;

namespace RedCodeApi.Endpoints;

public static class AeroportosEndpoints
{
    public static void MapAeroportosEndpoints(this WebApplication app, string connectionString)
    {
        app.MapGet("/api/aeroportos", async () =>
        {
            using var db = new SqliteConnection(connectionString);
            var aeroportos = await db.QueryAsync<Aeroporto>(
                "SELECT * FROM Aeroportos ORDER BY Cidade, Nome");
            return Results.Ok(aeroportos);
        });

        app.MapGet("/api/aeroportos/busca", async (string q) =>
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Results.BadRequest("Erro: Termo de busca deve ter pelo menos 2 caracteres.");

            using var db = new SqliteConnection(connectionString);
            var aeroportos = await db.QueryAsync<Aeroporto>(
                @"SELECT * FROM Aeroportos
                  WHERE Nome LIKE @Q OR Cidade LIKE @Q OR CodigoIATA LIKE @Q
                  ORDER BY Cidade, Nome",
                new { Q = $"%{q}%" });
            return Results.Ok(aeroportos);
        });
    }
}
