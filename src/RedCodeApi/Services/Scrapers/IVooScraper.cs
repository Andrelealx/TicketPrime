using RedCodeApi.Dtos.FlyCompare;

namespace RedCodeApi.Services.Scrapers;

/// <summary>
/// Interface para scrapers de passagens aereas (Strategy Pattern).
/// Cada implementacao busca voos de uma fonte especifica (companhia, agregador, etc).
/// </summary>
public interface IVooScraper
{
    /// <summary>
    /// Nome unico do scraper (ex: "latam", "gol", "azul", "decolar").
    /// Usado para identificar a fonte nos resultados e no cache.
    /// </summary>
    string Nome { get; }

    /// <summary>
    /// Prioridade de execucao (menor numero = executado primeiro).
    /// </summary>
    int Ordem { get; }

    /// <summary>
    /// Busca voos disponiveis para a rota e data especificadas.
    /// </summary>
    /// <param name="origem">Codigo IATA de origem (3 letras, ex: GRU).</param>
    /// <param name="destino">Codigo IATA de destino (3 letras, ex: REC).</param>
    /// <param name="dataPartida">Data do voo.</param>
    /// <param name="cancellationToken">Token de cancelamento para operacoes HTTP.</param>
    /// <returns>Lista de resultados de busca normalizados.</returns>
    Task<List<ResultadoBusca>> BuscarVoosAsync(
        string origem,
        string destino,
        DateTime dataPartida,
        CancellationToken cancellationToken = default);
}
