namespace WebAPIDevSecOpsScallingSDD.Services
{
    // Estados posibles de la saga de ventas en 06-01. El catálogo definitivo vive en 06-04.
    public static class SagaStates
    {
        public const string Creado = "Creado";
        public const string StockValidado = "StockValidado";
        public const string StockRechazado = "StockRechazado";
        public const string PagoProcesado = "PagoProcesado";
        public const string PagoRechazado = "PagoRechazado";
        public const string Facturado = "Facturado";
        public const string FacturaRechazada = "FacturaRechazada";
        public const string Cancelado = "Cancelado";
    }
}
