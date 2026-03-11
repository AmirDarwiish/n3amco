public class Customer
{
    public int Id { get; set; }

    public string Name { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }

    public decimal CurrentBalance { get; set; } // عليه كام

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ICollection<Sale> Sales { get; set; }
    public ICollection<CustomerPayment> Payments { get; set; }
    public ICollection<CustomerLedger> Ledger { get; set; }
}