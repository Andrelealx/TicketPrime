using RedCodeApi.Dtos.FlyCompare;

namespace RedCodeApi.Services.Scrapers;

/// <summary>
/// Scraper para Azul Linhas Aereas.
/// </summary>
public class ScraperAzul : ScraperBase<ScraperAzul>
{
    public override string Nome => "azul";
    public override int Ordem => 3;
    protected override string PrefixoVoo => "AD";
    protected override string NomeCompanhia => "AZUL";
    protected override string SiteBase => "https://www.voeazul.com.br/busca";
    protected override int DuracaoBaseMinutos => 190;
    protected override decimal TaxaImposto => 0.08m;
    protected override bool BagagemInclusa => true;
    protected override int HoraBasePartida => 8;

    public ScraperAzul(HttpClient httpClient, ILogger<ScraperAzul> logger)
        : base(httpClient, logger) { }
}
