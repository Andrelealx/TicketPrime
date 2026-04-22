using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using Dapper;
using System;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// CORS: permite que o front-end Blazor se comunique com a API
builder.Services.AddCors(options => {
    options.AddPolicy("BlazorPolicy", policy => {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// Le a string de conexao do appsettings (ou variavel ConnectionStrings__TicketPrime).
var connStr = builder.Configuration.GetConnectionString("TicketPrime");
if (string.IsNullOrWhiteSpace(connStr))
{
    throw new InvalidOperationException(
        "Connection string 'TicketPrime' nao configurada. Defina em appsettings ou na variavel ConnectionStrings__TicketPrime.");
}

var app = builder.Build();

app.UseCors("BlazorPolicy");


// ==========================================
// MÃ“DULO DE USUÃRIOS
// ==========================================
app.MapPost("/api/usuarios", async (Usuario user) => {
    // ID 34: Tamanho do CPF
    if (user.Cpf.Length != 11) return Results.BadRequest("Erro: CPF deve ter exatamente 11 caracteres.");

    // ID 32: ValidaÃ§Ã£o de E-mail com Regex
    if (!Regex.IsMatch(user.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) return Results.BadRequest("Erro: E-mail em formato invÃ¡lido.");

    using var db = new SqlConnection(connStr);

    // ID 02: Validar CPF Ãšnico (Fail-Fast)
    var existe = await db.QueryFirstOrDefaultAsync<int>("SELECT COUNT(1) FROM Usuarios WHERE Cpf = @Cpf", new { user.Cpf });
    if (existe > 0) return Results.BadRequest("Erro: CPF jÃ¡ cadastrado.");

    // Validar E-mail Ãºnico
    var emailExiste = await db.QueryFirstOrDefaultAsync<int>("SELECT COUNT(1) FROM Usuarios WHERE Email = @Email", new { user.Email });
    if (emailExiste > 0) return Results.BadRequest("Erro: E-mail jÃ¡ cadastrado por outro usuÃ¡rio.");

    // ID 01: Cadastro de UsuÃ¡rio
    await db.ExecuteAsync("INSERT INTO Usuarios (Cpf, Nome, Email) VALUES (@Cpf, @Nome, @Email)", user);
    return Results.Created($"/api/usuarios/{user.Cpf}", user);
});

// ==========================================
// MÃ“DULO DE EVENTOS
// ==========================================
app.MapPost("/api/eventos", async (Evento ev) => {
    // ID 30: Impedir datas passadas
    if (ev.DataEvento <= DateTime.Now) return Results.BadRequest("Erro: A data do evento nÃ£o pode ser no passado.");

    using var db = new SqlConnection(connStr);
    var id = await db.QuerySingleAsync<int>("INSERT INTO Eventos (Nome, CapacidadeTotal, DataEvento, PrecoPadrao) OUTPUT INSERTED.Id VALUES (@Nome, @CapacidadeTotal, @DataEvento, @PrecoPadrao)", ev);
    return Results.Created($"/api/eventos/{id}", ev);
});

// AV1 Item 5: GET /api/eventos â€” Listagem de todos os eventos
app.MapGet("/api/eventos", async () => {
    using var db = new SqlConnection(connStr);
    var eventos = await db.QueryAsync<Evento>("SELECT * FROM Eventos ORDER BY DataEvento");
    return Results.Ok(eventos);
});

// ID 05: Selecionar evento especÃ­fico
app.MapGet("/api/eventos/{id}", async (int id) => {
    using var db = new SqlConnection(connStr);
    var ev = await db.QueryFirstOrDefaultAsync<Evento>("SELECT * FROM Eventos WHERE Id = @Id", new { Id = id });
    return ev is not null ? Results.Ok(ev) : Results.NotFound("Evento nÃ£o encontrado.");
});

// ==========================================
// MÃ“DULO DE CUPONS
// ==========================================
app.MapPost("/api/cupons", async (Cupom cupom) => {
    if (string.IsNullOrWhiteSpace(cupom.Codigo)) return Results.BadRequest("Erro: CÃ³digo do cupom Ã© obrigatÃ³rio.");
    if (cupom.PorcentagemDesconto <= 0 || cupom.PorcentagemDesconto > 100) return Results.BadRequest("Erro: Porcentagem deve ser entre 1 e 100.");
    if (cupom.ValorMinimoRegra < 0) return Results.BadRequest("Erro: Valor mÃ­nimo nÃ£o pode ser negativo.");

    using var db = new SqlConnection(connStr);
    var existe = await db.QueryFirstOrDefaultAsync<int>("SELECT COUNT(1) FROM Cupons WHERE Codigo = @Codigo", new { cupom.Codigo });
    if (existe > 0) return Results.BadRequest("Erro: CÃ³digo de cupom jÃ¡ cadastrado.");

    await db.ExecuteAsync("INSERT INTO Cupons (Codigo, PorcentagemDesconto, ValorMinimoRegra) VALUES (@Codigo, @PorcentagemDesconto, @ValorMinimoRegra)", cupom);
    return Results.Created($"/api/cupons/{cupom.Codigo}", cupom);
});

// ID 35: Visualizar porcentagem e dados do cupom
app.MapGet("/api/cupons/{codigo}", async (string codigo) => {
    using var db = new SqlConnection(connStr);
    var cupom = await db.QueryFirstOrDefaultAsync<Cupom>("SELECT * FROM Cupons WHERE Codigo = @Codigo", new { Codigo = codigo });
    return cupom is not null ? Results.Ok(cupom) : Results.NotFound("Cupom nÃ£o encontrado.");
});

// ==========================================
// MÃ“DULO DE RESERVAS (O MOTOR PRINCIPAL)
// ==========================================
app.MapPost("/api/reservas", async (ReservaReq req) => {
    using var db = new SqlConnection(connStr);

    // ID 11 / R1 â€” Integridade: validar existÃªncia do UsuarioCpf
    var usuarioExiste = await db.QuerySingleAsync<int>("SELECT COUNT(1) FROM Usuarios WHERE Cpf = @Cpf", new { Cpf = req.UsuarioCpf });
    if (usuarioExiste == 0) return Results.BadRequest("Erro: UsuÃ¡rio com este CPF nÃ£o encontrado. Realize o cadastro antes de reservar.");

    // R1 â€” Integridade: validar existÃªncia do EventoId
    var ev = await db.QueryFirstOrDefaultAsync<Evento>("SELECT * FROM Eventos WHERE Id = @EventoId", new { req.EventoId });
    if (ev == null) return Results.NotFound("Erro: Evento nÃ£o encontrado.");

    // ID 12 / R3 â€” Bloquear Overbooking (Capacidade MÃ¡xima)
    var qtdReservas = await db.QuerySingleAsync<int>("SELECT COUNT(1) FROM Reservas WHERE EventoId = @EventoId", new { req.EventoId });
    if (qtdReservas >= ev.CapacidadeTotal) return Results.BadRequest("Erro CrÃ­tico: Overbooking. O evento jÃ¡ estÃ¡ lotado.");

    // ID 13 / R2 â€” Bloquear Cambistas: limite de 2 reservas por CPF no mesmo evento
    var temReserva = await db.QuerySingleAsync<int>("SELECT COUNT(1) FROM Reservas WHERE UsuarioCpf = @UsuarioCpf AND EventoId = @EventoId", req);
    if (temReserva >= 2) return Results.BadRequest("Erro CrÃ­tico: Limite excedido. Permitido no mÃ¡ximo 2 ingressos por CPF por evento.");

    decimal valorFinal = ev.PrecoPadrao;

    // ID 14: Reservas sem cupom (Null) vs ID 06: Aplicar cupom
    if (!string.IsNullOrEmpty(req.CupomUtilizado)) {
        // ID 07: Validar existÃªncia do cupom
        var cupom = await db.QueryFirstOrDefaultAsync<Cupom>("SELECT * FROM Cupons WHERE Codigo = @Codigo", new { Codigo = req.CupomUtilizado });
        if (cupom == null) return Results.BadRequest("Erro: Cupom inexistente.");

        // ID 08 / R4 â€” Motor de Cupons: aplica desconto somente se preÃ§o >= valor mÃ­nimo
        if (ev.PrecoPadrao < cupom.ValorMinimoRegra) return Results.BadRequest("Erro: O preÃ§o do evento Ã© menor que o exigido por este cupom.");

        // ID 10: Calcular valor final automÃ¡tico
        valorFinal = ev.PrecoPadrao - (ev.PrecoPadrao * (cupom.PorcentagemDesconto / 100));

        // ID 09: Impedir valor final negativo
        if (valorFinal < 0) valorFinal = 0;
    }

    // ID 31: Armazenar Valor Final Pago
    req.ValorFinalPago = valorFinal;

    // ID 11: Realizar reserva (OperaÃ§Ã£o de Insert com FKs)
    await db.ExecuteAsync("INSERT INTO Reservas (UsuarioCpf, EventoId, CupomUtilizado, ValorFinalPago) VALUES (@UsuarioCpf, @EventoId, @CupomUtilizado, @ValorFinalPago)", req);

    return Results.Created("/api/reservas", new { Mensagem = "Reserva concluÃ­da com sucesso!", ValorPago = valorFinal });
});

// HU-05: GET /api/reservas/{cpf} â€” Consultar reservas por CPF com JOIN
app.MapGet("/api/reservas/{cpf}", async (string cpf) => {
    using var db = new SqlConnection(connStr);
    var reservas = await db.QueryAsync<ReservaConsulta>(
        @"SELECT r.Id, r.UsuarioCpf, r.EventoId, e.Nome AS NomeEvento,
                 r.CupomUtilizado, r.ValorFinalPago, r.DataReserva
          FROM Reservas r
          INNER JOIN Eventos e ON r.EventoId = e.Id
          WHERE r.UsuarioCpf = @Cpf
          ORDER BY r.DataReserva DESC",
        new { Cpf = cpf });
    return Results.Ok(reservas);
});

app.Run();

// ==========================================
// CLASSES (MODELS)
// ==========================================
public class Usuario {
    public string Cpf { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class Evento {
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int CapacidadeTotal { get; set; }
    public DateTime DataEvento { get; set; }
    public decimal PrecoPadrao { get; set; }
}

public class Cupom {
    public string Codigo { get; set; } = string.Empty;
    public decimal PorcentagemDesconto { get; set; }
    public decimal ValorMinimoRegra { get; set; }
}

public class ReservaReq {
    public string UsuarioCpf { get; set; } = string.Empty;
    public int EventoId { get; set; }
    public string? CupomUtilizado { get; set; }
    public decimal ValorFinalPago { get; set; }
}

public class ReservaConsulta {
    public int Id { get; set; }
    public string UsuarioCpf { get; set; } = string.Empty;
    public int EventoId { get; set; }
    public string NomeEvento { get; set; } = string.Empty;
    public string? CupomUtilizado { get; set; }
    public decimal ValorFinalPago { get; set; }
    public DateTime DataReserva { get; set; }
}

