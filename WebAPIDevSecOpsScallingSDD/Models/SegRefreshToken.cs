using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAPIDevSecOpsScallingSDD.Models
{
    [Table("SegRefreshToken")]
    public class SegRefreshToken : IConcurrenteAuditable
    {
        [Key]
        public int id { get; set; }

        [Required]
        public int idSegUsuario { get; set; }

        [Required]
        [StringLength(64)]
        public string strTokenHash { get; set; } = null!;

        [Required]
        public DateTime dteExpiresAt { get; set; }

        [Required]
        public DateTime dteCreatedAt { get; set; }

        public DateTime? dteRevokedAt { get; set; }

        [StringLength(64)]
        public string? strReplacedByTokenHash { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = new byte[] { 1 };

        [ForeignKey("idSegUsuario")]
        public SegUsuario? SegUsuario { get; set; }
    }
}
