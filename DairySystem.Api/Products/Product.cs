using DairySystem.Api.Units;

public class Product
{
    public int Id { get; set; }

    // Basic
    public string Name { get; set; }
    public string Code { get; set; }

    // Classification
    public ProductType Type { get; set; }

    // Unit
    public int UnitId { get; set; }
    public decimal MinStock { get; set; } = 0; 

    // Pricing (defaults)
    public decimal DefaultPurchasePrice { get; set; }
    public decimal DefaultSellingPrice { get; set; }

    // Flags
    public bool IsRawMaterial { get; set; } // لبن خام
    public bool IsManufactured { get; set; } // منتج مصنع

    // Status
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    // Navigation
    public Unit Unit { get; set; }

    public ICollection<ProductBatch> Batches { get; set; } = new List<ProductBatch>();
}