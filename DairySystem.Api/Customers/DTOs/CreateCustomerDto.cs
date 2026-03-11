public class CreateCustomerDto
{
    public string Name { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
}

public class CustomerPaymentDto
{
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Notes { get; set; }
}