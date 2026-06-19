using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RedCodeTests;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Arrange
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GET_Aeroportos_AoChamarEndpoint_DeveRetornarListaNaoVazia()
    {
        // Act
        var response = await _client.GetAsync("/api/aeroportos");

        // Assert
        response.EnsureSuccessStatusCode();
        var aeroportos = await response.Content.ReadFromJsonAsync<List<object>>();
        Assert.NotNull(aeroportos);
        Assert.NotEmpty(aeroportos);
    }

    [Fact]
    public async Task GET_AeroportosBusca_ComTermoGRU_DeveConterTermoNaResposta()
    {
        // Act
        var response = await _client.GetAsync("/api/aeroportos/busca?q=GRU");

        // Assert
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("GRU", json);
    }

    [Fact]
    public async Task GET_Companhias_AoChamarEndpoint_DeveRetornarListaNaoVazia()
    {
        // Act
        var response = await _client.GetAsync("/api/companhias");

        // Assert
        response.EnsureSuccessStatusCode();
        var companhias = await response.Content.ReadFromJsonAsync<List<object>>();
        Assert.NotNull(companhias);
        Assert.NotEmpty(companhias);
    }

    [Fact]
    public async Task GET_RotasPopulares_AoChamarEndpoint_DeveRetornarListaNaoVazia()
    {
        // Act
        var response = await _client.GetAsync("/api/rotas/populares");

        // Assert
        response.EnsureSuccessStatusCode();
        var rotas = await response.Content.ReadFromJsonAsync<List<object>>();
        Assert.NotNull(rotas);
        Assert.NotEmpty(rotas);
    }

    [Fact]
    public async Task GET_BuscaVoos_ComCodigoIATAOrigemInvalido_DeveRetornarBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/voos/busca?origem=GR&destino=REC&dataPartida=2026-12-01");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_Alertas_ComEmailInvalido_DeveRetornarBadRequest()
    {
        // Arrange
        var body = new { email = "invalido", origem = "GRU", destino = "REC", precoAlvo = 500 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/alertas", body);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
}
