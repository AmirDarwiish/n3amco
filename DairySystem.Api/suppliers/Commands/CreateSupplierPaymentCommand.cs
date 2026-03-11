public class CreateSupplierPaymentCommand
{
    public int SupplierId { get; set; }

    public decimal Amount { get; set; }

    public int PaymentMethodId { get; set; }

    public string Notes { get; set; }
}