public class Sale
{
    public int Id { get; set; }

    public DateTime Date { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalCost { get; set; }
    public int? CustomerId { get; set; }
    public Customer Customer { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}