using Xunit;

namespace TicketPrimeTests;

public class CalculoCupomTests
{
    [Fact]
    public void ValidarCalculoMatematico_Desconto_DeveRetornarValorCorreto()
    {
        // Arrange (Preparação)
        decimal precoPadrao = 100.00m;
        decimal porcentagemDesconto = 20.00m; // 20%
        decimal valorEsperado = 80.00m;

        // Act (Ação - simulando a lógica do ID 10)
        decimal valorCalculado = precoPadrao - (precoPadrao * (porcentagemDesconto / 100));

        // Assert (O "Oráculo" - validando a verdade)
        Assert.Equal(valorEsperado, valorCalculado);
    }
}