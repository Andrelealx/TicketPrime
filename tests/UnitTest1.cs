using Xunit;
using Microsoft.Extensions.Logging;
using RedCodeApi.Dtos.FlyCompare;
using RedCodeApi.Services.Scrapers;

namespace RedCodeTests;

/// <summary>
/// Stub de ILogger que nao faz nada, usado nos testes do NormalizadorDados.
/// </summary>
internal sealed class NullLoggerStub<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}

public class FlyCompareTests
{
    #region NormalizadorDados — Normalizar

    [Fact]
    public void Normalizar_ListaVazia_DeveRetornarListaVazia()
    {
        // Arrange
        var normalizador = new NormalizadorDados(new NullLoggerStub<NormalizadorDados>());

        // Act
        var resultado = normalizador.Normalizar(new List<ResultadoBusca>());

        // Assert
        Assert.NotNull(resultado);
        Assert.Empty(resultado);
    }

    [Fact]
    public void Normalizar_ListaNula_DeveRetornarListaVazia()
    {
        // Arrange
        var normalizador = new NormalizadorDados(new NullLoggerStub<NormalizadorDados>());

        // Act
        var resultado = normalizador.Normalizar(null!);

        // Assert
        Assert.NotNull(resultado);
        Assert.Empty(resultado);
    }

    [Fact]
    public void Normalizar_Duplicatas_DeveManterApenasOMaisBarato()
    {
        // Arrange
        var normalizador = new NormalizadorDados(new NullLoggerStub<NormalizadorDados>());
        var voos = new List<ResultadoBusca>
        {
            new() { CodigoVoo = "LA1234", Companhia = "latam", PrecoTotal = 500.00m, Origem = "gru", Destino = "rec", Fonte = "latam" },
            new() { CodigoVoo = "LA1234", Companhia = "LATAM", PrecoTotal = 450.00m, Origem = "GRU", Destino = "REC", Fonte = "latam" },
            new() { CodigoVoo = "GZ5678", Companhia = "gol", PrecoTotal = 300.00m, Origem = "gru", Destino = "rec", Fonte = "gol" },
        };

        // Act
        var resultado = normalizador.Normalizar(voos);

        // Assert
        Assert.Equal(2, resultado.Count);
        var la1234 = resultado.First(r => r.CodigoVoo == "LA1234");
        Assert.Equal(450.00m, la1234.PrecoTotal);
    }

    [Fact]
    public void Normalizar_Ordenacao_DeveOrdenarPorPrecoTotalCrescente()
    {
        // Arrange
        var normalizador = new NormalizadorDados(new NullLoggerStub<NormalizadorDados>());
        var voos = new List<ResultadoBusca>
        {
            new() { CodigoVoo = "GZ001", Companhia = "gol", PrecoTotal = 500.00m, Origem = "gru", Destino = "rec", Fonte = "gol" },
            new() { CodigoVoo = "LA002", Companhia = "latam", PrecoTotal = 300.00m, Origem = "gru", Destino = "rec", Fonte = "latam" },
            new() { CodigoVoo = "AZ003", Companhia = "azul", PrecoTotal = 700.00m, Origem = "gru", Destino = "rec", Fonte = "azul" },
        };

        // Act
        var resultado = normalizador.Normalizar(voos);

        // Assert
        Assert.Equal(3, resultado.Count);
        Assert.Equal(300.00m, resultado[0].PrecoTotal);
        Assert.Equal(500.00m, resultado[1].PrecoTotal);
        Assert.Equal(700.00m, resultado[2].PrecoTotal);
    }

    [Fact]
    public void Normalizar_Padronizacao_DevePadronizarTodosOsCampos()
    {
        // Arrange
        var normalizador = new NormalizadorDados(new NullLoggerStub<NormalizadorDados>());
        var voos = new List<ResultadoBusca>
        {
            new()
            {
                CodigoVoo = "la-9876",
                Companhia = "latam",
                Origem = "Gru",
                Destino = "rec",
                Fonte = "LATAM",
                PrecoTotal = -350.00m,
                PrecoSemTaxas = -300.00m,
                Taxas = -50.00m,
                Paradas = -1,
                DuracaoMinutos = 0,
                TipoTarifa = "promocional",
                BagagemIncluida = true
            }
        };

        // Act
        var resultado = normalizador.Normalizar(voos);

        // Assert
        var voo = resultado[0];
        Assert.Equal("LA-9876", voo.CodigoVoo);
        Assert.Equal("LATAM", voo.Companhia);
        Assert.Equal("GRU", voo.Origem);
        Assert.Equal("REC", voo.Destino);
        Assert.Equal("latam", voo.Fonte);
        Assert.Equal(350.00m, voo.PrecoTotal);
        Assert.Equal(300.00m, voo.PrecoSemTaxas);
        Assert.Equal(50.00m, voo.Taxas);
        Assert.Equal(30, voo.DuracaoMinutos);
        Assert.Equal(0, voo.Paradas);
        Assert.Equal("Promo", voo.TipoTarifa);
    }

    [Fact]
    public void Normalizar_ComOutlier_DeveRemoverPrecoExtremo()
    {
        // Arrange
        var normalizador = new NormalizadorDados(new NullLoggerStub<NormalizadorDados>());
        var voos = new List<ResultadoBusca>();
        for (int i = 0; i < 20; i++)
        {
            voos.Add(new() { CodigoVoo = $"V{i:D2}", Companhia = "gol", PrecoTotal = 90m + i, Origem = "gru", Destino = "rec", Fonte = "gol" });
        }
        voos.Add(new() { CodigoVoo = "OUT", Companhia = "gol", PrecoTotal = 999999.00m, Origem = "gru", Destino = "rec", Fonte = "gol" });

        // Act
        var resultado = normalizador.Normalizar(voos);

        // Assert
        Assert.DoesNotContain(resultado, v => v.CodigoVoo == "OUT");
        Assert.Equal(20, resultado.Count);
    }

    [Fact]
    public void Normalizar_ComPoucosResultados_NaoDeveRemoverOutliers()
    {
        // Arrange
        var normalizador = new NormalizadorDados(new NullLoggerStub<NormalizadorDados>());
        var voos = new List<ResultadoBusca>
        {
            new() { CodigoVoo = "V01", Companhia = "gol", PrecoTotal = 100.00m, Origem = "gru", Destino = "rec", Fonte = "gol" },
            new() { CodigoVoo = "V02", Companhia = "gol", PrecoTotal = 999999.00m, Origem = "gru", Destino = "rec", Fonte = "gol" },
            new() { CodigoVoo = "V03", Companhia = "gol", PrecoTotal = 110.00m, Origem = "gru", Destino = "rec", Fonte = "gol" },
        };

        // Act
        var resultado = normalizador.Normalizar(voos);

        // Assert
        Assert.Equal(3, resultado.Count);
        Assert.Contains(resultado, v => v.CodigoVoo == "V02");
    }

    #endregion

    #region IATA — Validacao de Codigo de Aeroporto

    [Theory]
    [InlineData("GRU", true)]
    [InlineData("REC", true)]
    [InlineData("GIG", true)]
    [InlineData("CGH", true)]
    [InlineData("BSB", true)]
    [InlineData("GR", false)]
    [InlineData("GRUU", false)]
    [InlineData("grU", false)]
    [InlineData("", false)]
    [InlineData("123", false)]
    [InlineData("AB", false)]
    public void ValidarCodigoIATA_ComDiversosCodigos_DeveValidarFormatoCorretamente(string codigo, bool deveSerValido)
    {
        // Arrange
        bool Valido(string c) => !string.IsNullOrEmpty(c)
            && c.Length == 3
            && c.All(char.IsLetter)
            && c.All(char.IsUpper);

        // Act
        bool valido = Valido(codigo);

        // Assert
        Assert.Equal(deveSerValido, valido);
    }

    #endregion

    #region ResultadoBusca — Validacao

    [Fact]
    public void ResultadoBusca_ComObjetoPadrao_TodasAsStringsDevemSerNaoNulas()
    {
        // Arrange
        var voo = new ResultadoBusca();

        // Act & Assert
        Assert.NotNull(voo.CodigoVoo);
        Assert.NotNull(voo.Companhia);
        Assert.NotNull(voo.Origem);
        Assert.NotNull(voo.Destino);
        Assert.NotNull(voo.TipoTarifa);
        Assert.NotNull(voo.UrlCompra);
        Assert.NotNull(voo.Fonte);
    }

    #endregion
}
