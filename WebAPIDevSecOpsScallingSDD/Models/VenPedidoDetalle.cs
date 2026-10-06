using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAPIDevSecOpsScallingSDD.Models
{
    [Table("VenPedidoDetalle")]
    public class VenPedidoDetalle : IConcurrenteAuditable
    {
        [Key]
        public int id { get; set; }

        [Required]
        public Guid idVenPedido { get; set; }

        [Required]
        public int idProProducto { get; set; }

        [Required]
        public int intCantidad { get; set; }

        [Required]
        public decimal decPrecioUnitario { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = new byte[] { 1 };

        [ForeignKey("idVenPedido")]
        public VenPedido? VenPedido { get; set; }

        [ForeignKey("idProProducto")]
        public ProProducto? ProProducto { get; set; }
    }
}
