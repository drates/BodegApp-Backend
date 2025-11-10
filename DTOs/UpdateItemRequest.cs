using System.ComponentModel.DataAnnotations;

public class UpdateItemRequest
{
    
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public int Boxes { get; set; }

    public int UnitsPerBox { get; set; }
}
