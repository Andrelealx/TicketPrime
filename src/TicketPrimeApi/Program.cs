using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using Dapper;
using System;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ID 16: Conexão segura para uso com Dapper
const string connStr = @"Server=localhost\SQLEXPRESS;Database=TicketPrime;Trusted_Connection=True;TrustServerCertificate=True;";

// ==========================================
// MÓDULO DE USUÁRIOS
// ==========================================
app.MapPost("/api/usuarios", async (Usuario user) => {
    // ID 34: Tamanho do CPF
    if (user.Cpf.Length != 11) return Results.BadRequest("Erro: CPF deve ter exatamente 11 caracteres.");
    
    // ID 32: Validação de E-mail com Regex
    if (!Regex.IsMatch(user.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) return Results.BadRequest("Erro: E-mail em formato inválido.");
    
    using var db = new SqlConnection(connStr);
    
    // ID 02: Validar CPF Único (Fail-Fast)
    var existe = await db.QueryFirstOrDefaultAsync<int>("SELECT COUNT(1) FROM Usuarios WHERE Cpf = @Cpf", new { user.Cpf });
    if (existe > 0) return Results.BadRequest("Erro: CPF já cadastrado.");

    // ID 01: Cadastro de Usuário
    await db.ExecuteAsync("INSERT INTO Usuarios (Cpf, Nome, Email) VALUES (@Cpf, @Nome, @Email)", user);
    return Results.Created($"/api/usuarios/{user.Cpf}", user);
});

// ==========================================
// MÓDULO DE EVENTOS
// ==========================================
app.MapPost("/api/eventos", async (Evento ev) => {
    // ID 30: Impedir datas passadas
    if (ev.DataEvento <= DateTime.Now) return Results.BadRequest("Erro: A data do evento não pode ser no passado.");
    
    using var db = new SqlConnection(connStr);
    var id = await db.QuerySingleAsync<int>("INSERT INTO Eventos (Nome, CapacidadeTotal, DataEvento, PrecoPadrao) OUTPUT INSERTED.Id VALUES (@Nome, @CapacidadeTotal, @DataEvento, @PrecoPadrao)", ev);
    return Results.Created($"/api/eventos/{id}", ev);
});

// ID 05: Selecionar evento específico
app.MapGet("/api/eventos/{id}", async (int id) => {
    using var db = new SqlConnection(connStr);
    var ev = await db.QueryFirstOrDefaultAsync<Evento>("SELECT * FROM Eventos WHERE Id = @Id", new { Id = id });
    return ev is not null ? Results.Ok(ev) : Results.NotFound("Evento não encontrado.");
});

// ==========================================
// MÓDULO DE CUPONS
// ==========================================
// ID 35: Visualizar porcentagem do cupom
app.MapGet("/api/cupons/{codigo}", async (string codigo) => {
    using var db = new SqlConnection(connStr);
    var cupom = await db.QueryFirstOrDefaultAsync<Cupom>("SELECT * FROM Cupons WHERE Codigo = @Codigo", new { Codigo = codigo });
    return cupom is not null ? Results.Ok(new { cupom.PorcentagemDesconto }) : Results.NotFound("Cupom não encontrado.");
});

// ==========================================
// MÓDULO DE RESERVAS (O MOTOR PRINCIPAL)
// ==========================================
app.MapPost("/api/reservas", async (ReservaReq req) => {
    using var db = new SqlConnection(connStr);
    
    // Busca o Evento para cálculos
    var ev = await db.QueryFirstOrDefaultAsync<Evento>("SELECT * FROM Eventos WHERE Id = @EventoId", new { req.EventoId });
    if (ev == null) return Results.NotFound("Erro: Evento não encontrado.");

    // ID 12: Bloquear Overbooking (Capacidade Máxima)
    var qtdReservas = await db.QuerySingleAsync<int>("SELECT COUNT(1) FROM Reservas WHERE EventoId = @EventoId", new { req.EventoId });
    if (qtdReservas >= ev.CapacidadeTotal) return Results.BadRequest("Erro Crítico: Overbooking. O evento já está lotado.");

    // ID 13: Bloquear Cambistas (Apenas 1 ingresso por CPF no mesmo evento)
    var temReserva = await db.QuerySingleAsync<int>("SELECT COUNT(1) FROM Reservas WHERE CpfUsuario = @CpfUsuario AND EventoId = @EventoId", req);
    if (temReserva > 0) return Results.BadRequest("Erro Crítico: Limite excedido. Permitido apenas 1 ingresso por CPF por evento.");

    decimal valorFinal = ev.PrecoPadrao;

    // ID 14: Reservas sem cupom (Null) vs ID 06: Aplicar cupom
    if (!string.IsNullOrEmpty(req.CupomUtilizado)) {
        // ID 07: Validar existência do cupom
        var cupom = await db.QueryFirstOrDefaultAsync<Cupom>("SELECT * FROM Cupons WHERE Codigo = @Codigo", new { Codigo = req.CupomUtilizado });
        if (cupom == null) return Results.BadRequest("Erro: Cupom inexistente.");
        
        // ID 08: Cupom com valor mínimo
        if (ev.PrecoPadrao < cupom.ValorMinimoRegra) return Results.BadRequest("Erro: O preço do evento é menor que o exigido por este cupom.");
        
        // ID 10: Calcular valor final automático
        valorFinal = ev.PrecoPadrao - (ev.PrecoPadrao * (cupom.PorcentagemDesconto / 100));
        
        // ID 09: Impedir valor final negativo
        if (valorFinal < 0) valorFinal = 0; 
    }

    // ID 31: Armazenar Valor Final Pago
    req.ValorFinalPago = valorFinal;

    // ID 11: Realizar reserva (Operação de Insert com FKs)
    await db.ExecuteAsync("INSERT INTO Reservas (CpfUsuario, EventoId, CupomUtilizado, ValorFinalPago) VALUES (@CpfUsuario, @EventoId, @CupomUtilizado, @ValorFinalPago)", req);
    
    return Results.Ok(new { Mensagem = "Reserva concluída com sucesso!", ValorPago = valorFinal });
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
    public string CpfUsuario { get; set; } = string.Empty; 
    public int EventoId { get; set; } 
    public string? CupomUtilizado { get; set; } 
    public decimal ValorFinalPago { get; set; } 
}