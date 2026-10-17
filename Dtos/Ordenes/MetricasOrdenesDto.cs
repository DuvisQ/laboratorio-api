namespace Laboratorio.Api.Dtos.Ordenes
{
    public class MetricasOrdenesDto
    {
        public int TotalOrdenesHoy { get; set; }
        public int RegistradasHoy { get; set; }
        public int ProcesadasHoy { get; set; }
        public int ValidadasHoy { get; set; }
        public decimal TotalFacturadoUSDHoy { get; set; }
        public decimal TotalCobradoUSDHoy { get; set; }
        public decimal TotalCobradoVESHoy { get; set; }
    }
}
