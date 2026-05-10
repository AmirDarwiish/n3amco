using DairySystem.Api.suppliers;

public class SupplierPayment
{
    public int Id { get; set; }

    public int SupplierId { get; set; }

    public decimal Amount { get; set; }

    public int PaymentMethodId { get; set; }

    public DateTime Date { get; set; }

    public string Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public Supplier Supplier { get; set; }
}
