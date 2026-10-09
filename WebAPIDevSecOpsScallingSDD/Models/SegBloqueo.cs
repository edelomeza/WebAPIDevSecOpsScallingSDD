using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAPIDevSecOpsScallingSDD.Models
{
    /// <summary>Bloqueo persistente de login por nombre (04-02, 15 min tras 5 fallos). Cubre usuarios existentes e inexistentes.</summary>
    [Table("SegBloqueo")]
    public class SegBloqueo : IConcurrenteAuditable
    {
        [Key]
        public int id { get; set; }

        [StringLength(50)]
        public string strNombre { get; set; } = null!;

        public int intIntentosFallidos { get; set; }

        public DateTime? dteBloqueoHasta { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = new byte[] { 1 };
    }
}
