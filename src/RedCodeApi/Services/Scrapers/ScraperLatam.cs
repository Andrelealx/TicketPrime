using RedCodeApi.Dtos.FlyCompare;

namespace RedCodeApi.Services.Scrapers;

/// <summary>
/// Scraper para LATAM Airlines Brasil.
/// </summary>
public class ScraperLatam : ScraperBase<ScraperLatam>
{
    public override string Nome => "latam";
    public override int Ordem => 1;
    protected override string PrefixoVoo => "LA";
    protected override string NomeCompanhia => "LATAM";
    protected override string SiteBase => "https://www.latamairlines.com/br/pt/voos";
    protected override int DuracaoBaseMinutos => 180;
    protected override decimal TaxaImposto => 0.08m;
    protected override bool BagagemInclusa => true;
    protected override int HoraBasePartida => 6;

    public ScraperLatam(HttpClient httpClient, ILogger<ScraperLatam> logger)
        : base(httpClient, logger) { }
}
