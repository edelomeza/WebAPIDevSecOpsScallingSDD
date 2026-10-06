using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAPIDevSecOpsScallingSDD.Models
{
    [Table("VenVentaDetalle")]
    public class VenVentaDetalle : IConcurrenteAuditable
    {
        [Key]
        public int id { get; set; }

        public int idVenVenta { get; set; }

        public int idProProducto { get; set; }

        [Required]
        public int intPiezaVenta { get; set; }

        [Required]
        public decimal decTotalVenta { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = new byte[] { 1 };

        [ForeignKey("idVenVenta")]
        public VenVenta? VenVenta { get; set; }

        [ForeignKey("idProProducto")]
        public ProProducto? ProProducto { get; set; }
    }
}
