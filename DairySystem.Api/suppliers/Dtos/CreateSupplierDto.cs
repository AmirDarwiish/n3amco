public class CreateSupplierDto
{
    public string Name { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public decimal OpeningBalance { get; set; }
}

public class UpdateSupplierDto
{
    public string Name { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
}