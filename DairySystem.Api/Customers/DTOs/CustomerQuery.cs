public class CustomerQuery
{
    public string? Search { get; set; }

    public decimal? MinBalance { get; set; }
    public decimal? MaxBalance { get; set; }

    public bool? IsActive { get; set; } = true;

    public string SortBy { get; set; } = "createdAt"; // name / balance
    public string SortDir { get; set; } = "desc"; // asc / desc

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}