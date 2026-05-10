using n3amco.Api.suppliers;

public class SupplierLedger
{
    public int Id { get; set; }

    public int SupplierId { get; set; }

    public decimal Amount { get; set; } // + لبن / - دفع

    public SupplierTransactionType Type { get; set; }

    public DateTime Date { get; set; }

    public int? ReferenceId { get; set; }

    public string Notes { get; set; }

    // Navigation
    public Supplier Supplier { get; set; }
}
public enum SupplierTransactionType
{
    MilkCollection = 1,
    Payment = 2
}