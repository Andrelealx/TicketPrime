namespace RedCodeApi.Models.FlyCompare;

public class Voo
{
    public int Id { get; set; }
    public int RotaId { get; set; }
    public int CompanhiaId { get; set; }
    public string CodigoVoo { get; set; } = string.Empty;
    public DateTime DataPartida { get; set; }
    public DateTime DataChegada { get; set; }
    public int DuracaoMinutos { get; set; }
    public int Paradas { get; set; }
    public int? AeroportoEscalaId { get; set; }
    public string Classe { get; set; } = "Econômica";
}
