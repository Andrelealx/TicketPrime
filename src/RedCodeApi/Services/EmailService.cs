using System.Net;
using System.Net.Mail;

namespace RedCodeApi.Services;

/// <summary>
/// Servico de envio de emails para notificacoes de alertas de preco (LOW-01).
/// Configurado via appsettings.json (secao "Smtp").
/// Se nao configurado, loga as notificacoes como fallback.
/// </summary>
public class EmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly bool _habilitado;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _habilitado = !string.IsNullOrWhiteSpace(configuration["Smtp:Host"]);
        
        if (_habilitado)
            _logger.LogInformation("[Email] Servico SMTP configurado: {Host}:{Port}", 
                configuration["Smtp:Host"], configuration["Smtp:Port"]);
        else
            _logger.LogInformation("[Email] SMTP nao configurado. Notificacoes serao apenas logadas.");
    }

    /// <summary>
    /// Envia email de notificacao de alerta de preco disparado.
    /// </summary>
    public async Task EnviarNotificacaoAlertaAsync(
        string emailDestino,
        string origem,
        string destino,
        decimal precoAtual,
        decimal precoAlvo)
    {
        var assunto = $"🔔 FlyCompare — Alerta de Preço: {origem} → {destino} por R$ {precoAtual:F2}!";
        var corpo = $"""
            <html>
            <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <div style="background: linear-gradient(135deg, #0284C7, #0EA5E9); padding: 24px; border-radius: 12px 12px 0 0;">
                    <h1 style="color: white; margin: 0;">✈️ FlyCompare</h1>
                    <p style="color: rgba(255,255,255,0.9); margin: 8px 0 0;">Alerta de Preço Disparado!</p>
                </div>
                <div style="background: #F8FAFC; padding: 24px; border: 1px solid #E2E8F0; border-radius: 0 0 12px 12px;">
                    <h2 style="color: #1E293B; margin-top: 0;">Boa notícia! 🎉</h2>
                    <p style="color: #475569; font-size: 16px;">
                        O preço da passagem <strong>{origem} → {destino}</strong> atingiu <strong>R$ {precoAtual:F2}</strong>,
                        abaixo do seu alvo de <strong>R$ {precoAlvo:F2}</strong>.
                    </p>
                    <div style="background: #ECFDF5; border: 1px solid #A7F3D0; padding: 16px; border-radius: 8px; margin: 16px 0;">
                        <table style="width: 100%;">
                            <tr>
                                <td style="color: #64748B;">Preço Atual:</td>
                                <td style="text-align: right; font-weight: bold; color: #059669; font-size: 20px;">R$ {precoAtual:F2}</td>
                            </tr>
                            <tr>
                                <td style="color: #64748B;">Seu Alvo:</td>
                                <td style="text-align: right; color: #64748B;">R$ {precoAlvo:F2}</td>
                            </tr>
                            <tr>
                                <td style="color: #64748B;">Economia:</td>
                                <td style="text-align: right; font-weight: bold; color: #059669;">R$ {(precoAlvo - precoAtual):F2}</td>
                            </tr>
                        </table>
                    </div>
                    <a href="http://localhost:5139/flycompare" style="display: inline-block; background: #0EA5E9; color: white; padding: 12px 24px; border-radius: 8px; text-decoration: none; font-weight: bold;">
                        Buscar Passagens →
                    </a>
                    <p style="color: #94A3B8; font-size: 12px; margin-top: 16px;">
                        Este é um email automático do FlyCompare. Para não receber mais alertas, acesse Meus Alertas.
                    </p>
                </div>
            </body>
            </html>
            """;

        if (!_habilitado)
        {
            _logger.LogWarning(
                "[Email] SMTP nao configurado. Notificacao NAO enviada para {Email}: {Origem}->{Destino} R${Preco}",
                emailDestino, origem, destino, precoAtual);
            return;
        }

        try
        {
            using var smtp = new SmtpClient
            {
                Host = _configuration["Smtp:Host"]!,
                Port = int.Parse(_configuration["Smtp:Port"] ?? "587"),
                EnableSsl = bool.Parse(_configuration["Smtp:EnableSsl"] ?? "true"),
                Credentials = new NetworkCredential(
                    _configuration["Smtp:Username"] ?? string.Empty,
                    _configuration["Smtp:Password"] ?? string.Empty)
            };

            using var mensagem = new MailMessage
            {
                From = new MailAddress(
                    _configuration["Smtp:From"] ?? "alertas@flycompare.com.br",
                    "FlyCompare Alertas"),
                Subject = assunto,
                Body = corpo,
                IsBodyHtml = true
            };
            mensagem.To.Add(emailDestino);

            await smtp.SendMailAsync(mensagem);

            _logger.LogInformation(
                "[Email] Notificacao enviada com sucesso para {Email}: {Origem}->{Destino} R${Preco}",
                emailDestino, origem, destino, precoAtual);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Email] Falha ao enviar notificacao para {Email}: {Origem}->{Destino}",
                emailDestino, origem, destino);
        }
    }
}
