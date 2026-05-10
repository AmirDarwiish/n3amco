public interface ISaleService
{
    Task<int> CreateSaleAsync(CreateSaleCommand request);
}