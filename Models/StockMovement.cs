using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace BodegApp.Backend.Models
{
    public class StockMovement
    {
        public int Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string ProductCode { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Action { get; set; } = string.Empty;

        public int BoxesAfterChange { get; set; }

        [Range(-10000, 10000)]
        public int Delta { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public int UnitsPerBox { get; set; }

        // 🚨 CORRECCIÓN CRÍTICA: Cambiado de 'int?' a 'Guid?'
        public Guid? BatchId { get; set; } 

        [ForeignKey("BatchId")]
        // El tipo de navegación no necesita cambiar, pero es bueno asegurarse de que ItemBatch.cs usa Guid para su Id.
        public ItemBatch? Batch { get; set; } 

        public string ProductName { get; set; } = string.Empty;
    }
}