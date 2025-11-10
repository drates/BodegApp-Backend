using System.ComponentModel.DataAnnotations;
using BodegApp.Backend.DTOs;


public class CreateStockMovementRequest
{
    [Required]
    public string ProductCode { get; set; } = string.Empty;

    [Required]
    [RegularExpression("Ingreso|Egreso", ErrorMessage = "La acción debe ser 'Ingreso' o 'Egreso'.")]
    public string Action { get; set; } = string.Empty;

    [Range(-10000, 10000, ErrorMessage = "Delta debe estar entre -10.000 y 10.000.")]
    public int Delta { get; set; }

    [Range(0, 10000, ErrorMessage = "BoxesAfterChange debe estar entre 0 y 10.000.")]
    public int BoxesAfterChange { get; set; }

    [Range(1, 10000, ErrorMessage = "UnitsPerBox debe ser mayor que 0.")]
    public int? UnitsPerBox { get; set; }

    public Guid? BatchId { get; set; }
}
