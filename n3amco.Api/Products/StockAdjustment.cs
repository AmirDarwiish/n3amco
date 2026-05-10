public class StockAdjustment
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }      
    public AdjustmentType Type { get; set; }
    public string Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }      

    public Product Product { get; set; }
}

public enum AdjustmentType
{
    Add = 1,   // إضافة للمخزون
    Remove = 2    // خصم من المخزون
}