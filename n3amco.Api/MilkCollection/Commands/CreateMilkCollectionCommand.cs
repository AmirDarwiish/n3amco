public class CreateMilkCollectionCommand
{
    public int SupplierId { get; set; }
    public int ProductId { get; set; }

    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
}