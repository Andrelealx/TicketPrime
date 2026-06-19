using RedCodeApi.Dtos.FlyCompare;

namespace RedCodeApi.Services.Scrapers;

/// <summary>
/// Scraper para GOL Linhas Aereas.
/// </summary>
public class ScraperGol : ScraperBase<ScraperGol>
{
    public override string Nome => "gol";
    public override int Ordem => 2;
    protected override string PrefixoVoo => "G3";
    protected override string NomeCompanhia => "GOL";
    protected override string SiteBase => "https://www.voegol.com.br/busca";
    protected override int DuracaoBaseMinutos => 185;
    protected override decimal TaxaImposto => 0.08m;
    protected override bool BagagemInclusa => false;
    protected override int HoraBasePartida => 7;

    public ScraperGol(HttpClient httpClient, ILogger<ScraperGol> logger)
        : base(httpClient, logger) { }
}
