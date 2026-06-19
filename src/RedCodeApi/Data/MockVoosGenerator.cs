using RedCodeApi.Dtos.FlyCompare;

namespace RedCodeApi.Data;

/// <summary>
/// Gera dados mockados de voos como fallback quando os scrapers falham.
/// </summary>
public static class MockVoosGenerator
{
    public static List<ResultadoBusca> Gerar(string origem, string destino, DateTime dataPartida)
    {
        var seed = HashCode.Combine(origem, destino, dataPartida.DayOfYear);
        var random = new Random(seed);
        var mockVoos = new List<ResultadoBusca>();

        string[] companhias = { "LATAM", "GOL", "AZUL" };
        string[] prefixos = { "LA", "G3", "AD" };
        int[] duracoesBase = { 180, 175, 185 };

        for (int i = 0; i < 6; i++)
        {
            int compIndex = i % 3;
            int variante = i / 3;
            string codigo = $"{prefixos[compIndex]}{random.Next(3000, 9999)}";
            int duracao = duracoesBase[compIndex] + random.Next(-10, 10) + (variante * 20);
            decimal precoBase = random.Next(300, 1500);
            decimal taxas = Math.Round(precoBase * 0.1m, 2);
            bool bagagem = i % 2 == 0;

            mockVoos.Add(new ResultadoBusca
            {
                CodigoVoo = codigo,
                Companhia = companhias[compIndex],
                Origem = origem,
                Destino = destino,
                Partida = dataPartida.AddHours(6 + i * 2),
                Chegada = dataPartida.AddHours(6 + i * 2).AddMinutes(duracao),
                DuracaoMinutos = duracao,
                Paradas = variante,
                PrecoTotal = Math.Round(precoBase + taxas, 2),
                PrecoSemTaxas = precoBase,
                Taxas = taxas,
                TipoTarifa = bagagem ? "Economica" : "Promo",
                BagagemIncluida = bagagem,
                UrlCompra = $"https://www.{companhias[compIndex].ToLower()}.com.br/busca?origem={origem}&destino={destino}",
                Fonte = $"mock-{companhias[compIndex].ToLower()}"
            });
        }

        return mockVoos.OrderBy(v => v.PrecoTotal).ToList();
    }
}
