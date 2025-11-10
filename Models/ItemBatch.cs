using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BodegApp.Backend.Models
{
    public class ItemBatch
    {
        // La clave primaria (está bien como Guid)
        public Guid Id { get; set; } 

        [Required]
        [StringLength(50)]
        public string ProductCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0, int.MaxValue)] // Permitimos 0 cajas si es necesario, aunque en inventario suele ser 1+
        public int Boxes { get; set; }

        [Range(1, int.MaxValue)]
        public int UnitsPerBox { get; set; }

        // ✅ CORRECCIÓN CRÍTICA: Convertimos UserId a GUID NULLABLE (Guid?).
        // Esto permite que la columna acepte NULL y resuelve el FormatException de SQLite.
        public Guid? UserId { get; set; } // Ahora acepta null

        [ForeignKey("UserId")]
        // La propiedad de navegación también debe ser nullable
        public User? User { get; set; }

        [Required] // Aseguramos que los lotes deben estar en alguna bodega
        public Guid WarehouseId { get; set; }

        [ForeignKey("WarehouseId")]
        public Warehouse Warehouse { get; set; } = null!; // Se asume que no es nulo en la DB

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Computed property
        public int TotalUnits => Boxes * UnitsPerBox;
    }
}