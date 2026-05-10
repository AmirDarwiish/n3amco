public class StockAdjustmentDto
{
    public decimal Quantity { get; set; }       
    public AdjustmentType Type { get; set; }
    public string Reason { get; set; }
    public DateTime? ExpiryDate { get; set; }
}