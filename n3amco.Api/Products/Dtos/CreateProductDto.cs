public class CreateProductDto
{
    public string Name { get; set; }
    public string Code { get; set; }

    public ProductType Type { get; set; }

    public int UnitId { get; set; }
    public decimal MinStock { get; set; } = 0;


    public decimal DefaultPurchasePrice { get; set; }
    public decimal DefaultSellingPrice { get; set; }

    public DateTime? OpeningExpiryDate { get; set; }

    public bool IsRawMaterial { get; set; }
    public bool IsManufactured { get; set; }

    public decimal? OpeningQuantity { get; set; }
}
public class UpdateProductDto
{
    public string Name { get; set; }
    public string Code { get; set; }

    public int UnitId { get; set; }
    public decimal MinStock { get; set; } = 0;


    public decimal DefaultPurchasePrice { get; set; }
    public decimal DefaultSellingPrice { get; set; }
}
public class AddBatchDto
{
    public decimal Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public DateTime? ExpiryDate { get; set; }

    // 🔥 مهم في السيستم الحقيقي
    public int? ReferenceId { get; set; }
}