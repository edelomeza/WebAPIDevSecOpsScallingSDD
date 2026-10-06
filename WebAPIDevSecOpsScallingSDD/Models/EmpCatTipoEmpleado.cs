using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAPIDevSecOpsScallingSDD.Models
{
    [Table("EmpCatTipoEmpleado")]
    public class EmpCatTipoEmpleado
    {
        [Key]
        public int id { get; set; }

        [StringLength(50)]
        public string strValor { get; set; } = null!;

        [StringLength(150)]
        public string strDescripcion { get; set; } = null!;
    }
}
