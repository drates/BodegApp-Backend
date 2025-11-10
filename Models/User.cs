using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace BodegApp.Backend.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [NotMapped]
        public string UserName => Email;


        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string NombreEmpresa { get; set; } = string.Empty;

        [Required]
        public string TipoNegocio { get; set; } = string.Empty; // Ej: "Ferretería", "Farmacia", etc.


        public string Role { get; set; } = "User"; // valores posibles: "User", "Superadmin", "AdminCliente"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


        public Guid? DefaultWarehouseId { get; set; }

        [ForeignKey("DefaultWarehouseId")]
        public Warehouse? DefaultWarehouse { get; set; }

        public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();


    }
}
