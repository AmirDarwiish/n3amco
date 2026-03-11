public class CreateSaleCommand
{
    public List<SaleItemDto> Items { get; set; }
    public int? CustomerId { get; set; }
    public bool IsPaid { get; set; } = false; // ← أضف ده

}

public class SaleItemDto
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
}