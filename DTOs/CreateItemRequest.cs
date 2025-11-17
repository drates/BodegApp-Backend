using System.ComponentModel.DataAnnotations;
using BodegApp.Backend.DTOs;


namespace BodegApp.Backend.DTOs
{
    public class CreateItemRequest
    {
        [Required]
        [StringLength(50)]
        public string ProductCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Este producto no existe en el inventario. Debes ingresar un nombre.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Debe ser al menos 1 caja")]
        public int Boxes { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe ser al menos 1 unidad por caja")]
        public int UnitsPerBox { get; set; }
    }
}
