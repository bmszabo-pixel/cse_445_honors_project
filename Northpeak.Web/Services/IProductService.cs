using Northpeak.Web.Models;

namespace Northpeak.Web.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken = default);
}
