using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAPIDevSecOpsScallingSDD.Models
{
    // Idempotencia del bus (06-01): un redelivery del mismo (evento, pedido) se descarta.
    // UNIQUE(strNombreEvento, idPedido); InMemory no lo impone → pre-chequeo + catch en consumers.
    [Table("VenEventoProcesado")]
    public class VenEventoProcesado
    {
        [Key]
        public int id { get; set; }

        [Required]
        [StringLength(100)]
        public string strNombreEvento { get; set; } = null!;

        [Required]
        public Guid idPedido { get; set; }

        [Required]
        public DateTime dteFechaProcesado { get; set; }
    }
}
