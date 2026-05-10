public class CustomerLedger
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public decimal Amount { get; set; } // + بيع / - دفع

    public CustomerTransactionType Type { get; set; }

    public DateTime Date { get; set; }

    public int? ReferenceId { get; set; }

    public string Notes { get; set; }

    public Customer Customer { get; set; }
}
public enum CustomerTransactionType
{
    Sale = 1,
    Payment = 2
}