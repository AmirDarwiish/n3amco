public class CustomerPayment
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    public string Notes { get; set; }

    public Customer Customer { get; set; }
}