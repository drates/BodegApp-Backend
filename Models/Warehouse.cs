using System.ComponentModel.DataAnnotations.Schema;

using BodegApp.Backend.Models;



public class Warehouse
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ItemBatch> ItemBatches { get; set; } = new List<ItemBatch>();

}
