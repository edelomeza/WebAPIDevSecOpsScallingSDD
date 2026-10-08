using System;
using System.Collections.Generic;

namespace WebAPIDevSecOpsScallingSDD.Dtos
{
    public sealed class DashboardFilterDto
    {
        public DateTime? Desde { get; set; }

        public DateTime? Hasta { get; set; }

        public string? EstadoSaga { get; set; }
    }

    public sealed class EstadoConteoDto
    {
        public string Estado { get; set; } = string.Empty;

        public int Total { get; set; }
    }

    public sealed class DashboardDto
    {
        public int TotalPedidos { get; set; }

        public int TotalPagos { get; set; }

        public int TotalFacturas { get; set; }

        public decimal MontoTotalPedidos { get; set; }

        public decimal MontoTotalPagos { get; set; }

        public decimal MontoTotalFacturas { get; set; }

        public IReadOnlyList<EstadoConteoDto> PorEstadoSaga { get; set; } = [];

        public int ProfundidadCola { get; set; }
    }
}
