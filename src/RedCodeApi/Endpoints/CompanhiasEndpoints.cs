using Dapper;
using Microsoft.Data.Sqlite;
using RedCodeApi.Models.FlyCompare;

namespace RedCodeApi.Endpoints;

public static class CompanhiasEndpoints
{
    public static void MapCompanhiasEndpoints(this WebApplication app, string connectionString)
    {
        app.MapGet("/api/companhias", async () =>
        {
            using var db = new SqliteConnection(connectionString);
            var companhias = await db.QueryAsync<CompanhiaAerea>(
                "SELECT * FROM CompanhiasAereas WHERE Ativo = 1 ORDER BY Nome");
            return Results.Ok(companhias);
        });
    }
}
