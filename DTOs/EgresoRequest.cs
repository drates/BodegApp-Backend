using System.ComponentModel.DataAnnotations;

namespace BodegApp.Backend.DTOs
{
    public class EgresoRequest
    {
        [Required]
        [StringLength(50)]
        public string ProductCode { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Debe retirar al menos 1 caja")]
        public int Boxes { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Unidades por caja inválidas")]
        public int UnitsPerBox { get; set; }
    }
}
