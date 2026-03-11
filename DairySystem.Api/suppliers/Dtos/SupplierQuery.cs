public class SupplierQuery
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; } = true;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}