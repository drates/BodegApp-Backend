using System; // Necesario para Guid

namespace BodegApp.Backend.DTOs
{
    public class ItemBatchDto
    {
        // 🚨 CORRECCIÓN CRÍTICA: Cambiado de 'int' a 'Guid'
        public Guid Id { get; set; } 
        
        public string ProductCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Boxes { get; set; }
        public int UnitsPerBox { get; set; }
        public int TotalUnits => Boxes * UnitsPerBox;
    }
}