using Arcana.Core.Common;
using Arcana.Data.Local;
using Arcana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arcana.Data.Dao.Impl;

/// <summary>
/// EF Core implementation of OrderDao.
/// All direct AppDbContext / LINQ-to-EF calls are isolated here.
/// </summary>
public class OrderDaoImpl : OrderDao
{
    private readonly AppDbContext _context;

    public OrderDaoImpl(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> FindByIdWithItems(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items.Where(i => !i.IsDeleted))
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<Order?> FindByOrderNumber(string orderNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> FindByCustomerId(int customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> FindByDateRange(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Where(o => o.OrderDate >= from && o.OrderDate <= to)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> FindByStatus(OrderStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<string> GenerateOrderNumber(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var prefix = $"ORD{today:yyyyMMdd}";

        var lastOrder = await _context.Orders
            .IgnoreQueryFilters()
            .Where(o => o.OrderNumber.StartsWith(prefix))
            .OrderByDescending(o => o.OrderNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastOrder == null)
        {
            return $"{prefix}0001";
        }

        var lastNumber = int.Parse(lastOrder.OrderNumber[prefix.Length..]);
        return $"{prefix}{(lastNumber + 1):D4}";
    }

    public async Task<PagedResult<Order>> FindPagedWithFilters(
        PageRequest request,
        OrderStatus? status = null,
        int? customerId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(o => o.CustomerId == customerId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(o => o.OrderDate <= toDate.Value);
        }

        // Apply sorting
        query = string.IsNullOrEmpty(request.SortBy)
            ? query.OrderByDescending(o => o.OrderDate)
            : request.Descending
                ? query.OrderByDescending(e => EF.Property<object>(e, request.SortBy))
                : query.OrderBy(e => EF.Property<object>(e, request.SortBy));

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Order>(items, request.Page, request.PageSize, totalCount);
    }
}
