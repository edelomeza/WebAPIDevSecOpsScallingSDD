using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAPIDevSecOpsScallingSDD.Models
{
    [Table("VenCatEstado")]
    public class VenCatEstado
    {
        [Key]
        public int id { get; set; }

        [StringLength(50)]
        public string strValor { get; set; } = null!;

        [StringLength(200)]
        public string? strDescripcion { get; set; }
    }
}
