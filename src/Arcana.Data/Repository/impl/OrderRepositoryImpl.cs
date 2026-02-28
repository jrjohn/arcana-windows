using Arcana.Core.Common;
using Arcana.Data.Dao;
using Arcana.Data.Local;
using Arcana.Domain.Entities;

namespace Arcana.Data.Repository.Impl;

/// <summary>
/// Order repository implementation.
/// Generic CRUD operations are provided by the base RepositoryImpl class.
/// Order-specific queries are delegated to OrderDao.
/// </summary>
public class OrderRepositoryImpl : RepositoryImpl<Order>, OrderRepository
{
    private readonly OrderDao _dao;

    public OrderRepositoryImpl(AppDbContext context, OrderDao dao) : base(context)
    {
        _dao = dao;
    }

    public Task<Order?> GetByIdWithItemsAsync(int id, CancellationToken cancellationToken = default)
        => _dao.FindByIdWithItems(id, cancellationToken);

    public Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        => _dao.FindByOrderNumber(orderNumber, cancellationToken);

    public Task<IReadOnlyList<Order>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
        => _dao.FindByCustomerId(customerId, cancellationToken);

    public Task<IReadOnlyList<Order>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
        => _dao.FindByDateRange(from, to, cancellationToken);

    public Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
        => _dao.FindByStatus(status, cancellationToken);

    public Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default)
        => _dao.GenerateOrderNumber(cancellationToken);

    public Task<PagedResult<Order>> GetPagedWithFiltersAsync(
        PageRequest request,
        OrderStatus? status = null,
        int? customerId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
        => _dao.FindPagedWithFilters(request, status, customerId, fromDate, toDate, cancellationToken);
}
