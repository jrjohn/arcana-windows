using Arcana.Core.Common;
using Arcana.Data.Local;
using Arcana.Data.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Arcana.Sync.Tests.Repository;

public class UnitOfWorkTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ServiceProvider _serviceProvider;

    public UnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        var services = new ServiceCollection();
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _context.Dispose();
        _serviceProvider.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Act
        using var uow = new UnitOfWork(_context, _serviceProvider);

        // Assert
        uow.HasChanges.Should().BeFalse();
        uow.IsCommitted.Should().BeFalse();
        uow.IsRolledBack.Should().BeFalse();
    }

    #endregion

    #region GetRepository Tests

    [Fact]
    public void GetRepository_ShouldReturnRepository()
    {
        // Arrange
        using var uow = new UnitOfWork(_context, _serviceProvider);

        // Act
        var repo = uow.GetRepository<TestEntity>();

        // Assert
        repo.Should().NotBeNull();
    }

    [Fact]
    public void GetRepository_SameTwice_ShouldReturnSameInstance()
    {
        // Arrange
        using var uow = new UnitOfWork(_context, _serviceProvider);

        // Act
        var repo1 = uow.GetRepository<TestEntity>();
        var repo2 = uow.GetRepository<TestEntity>();

        // Assert
        repo1.Should().BeSameAs(repo2);
    }

    [Fact]
    public void GetRepository_WithKey_ShouldReturnRepository()
    {
        // Arrange
        using var uow = new UnitOfWork(_context, _serviceProvider);

        // Act
        var repo = uow.GetRepository<TestEntity, int>();

        // Assert
        repo.Should().NotBeNull();
    }

    #endregion

    #region HasChanges Tests

    [Fact]
    public async Task HasChanges_AfterAdd_ShouldBeTrue()
    {
        // Arrange
        using var uow = new UnitOfWork(_context, _serviceProvider);
        var entity = new TestEntity { Name = "Test" };

        // Act
        _context.Set<TestEntity>().Add(entity);

        // Assert
        uow.HasChanges.Should().BeTrue();
    }

    [Fact]
    public async Task HasChanges_AfterCommit_ShouldBeFalse()
    {
        // Arrange
        using var uow = new UnitOfWork(_context, _serviceProvider);
        var entity = new TestEntity { Name = "Test" };
        _context.Set<TestEntity>().Add(entity);
        uow.HasChanges.Should().BeTrue();

        // Act
        await uow.CommitAsync();

        // Assert
        uow.HasChanges.Should().BeFalse();
    }

    #endregion

    #region CommitAsync Tests

    [Fact]
    public async Task CommitAsync_ShouldSaveChanges()
    {
        // Arrange
        using var uow = new UnitOfWork(_context, _serviceProvider);
        var entity = new TestEntity { Name = "Test" };
        _context.Set<TestEntity>().Add(entity);

        // Act
        var result = await uow.CommitAsync();

        // Assert
        result.Should().Be(1);
        uow.IsCommitted.Should().BeTrue();
    }

    [Fact]
    public async Task CommitAsync_Twice_ShouldThrow()
    {
        // Arrange
        using var uow = new UnitOfWork(_context, _serviceProvider);
        await uow.CommitAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
    }

    [Fact]
    public async Task CommitAsync_AfterRollback_ShouldThrow()
    {
        // Arrange
        using var uow = new UnitOfWork(_context, _serviceProvider);
        await uow.RollbackAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
    }

    [Fact]
    public async Task CommitAsync_ShouldSetAuditFields_ForNewEntities()
    {
        // Arrange
        using var uow = new UnitOfWork(_context, _serviceProvider);
        var entity = new AuditableTestEntity { Name = "Test" };
        _context.Set<AuditableTestEntity>().Add(entity);

        var beforeCommit = DateTime.UtcNow;

        // Act
        await uow.CommitAsync();

        var afterCommit = DateTime.UtcNow;

        // Assert
        entity.CreatedAt.Should().BeOnOrAfter(beforeCommit);
        entity.CreatedAt.Should().BeOnOrBefore(afterCommit);
        entity.ModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CommitAsync_ShouldUpdateAuditFields_ForModifiedEntities()
    {
        // Arrange
        using var uow = new UnitOfWork(_context, _serviceProvider);
        var entity = new AuditableTestEntity { Name = "Test", CreatedAt = DateTime.UtcNow.AddDays(-1), ModifiedAt = DateTime.UtcNow.AddDays(-1) };
        _context.Set<AuditableTestEntity>().Add(entity);
        await _context.SaveChangesAsync();

        var originalCreatedAt = entity.CreatedAt;
        entity.Name = "Modified";
        _context.Set<AuditableTestEntity>().Update(entity);

        var beforeCommit = DateTime.UtcNow;

        // Act
        await uow.CommitAsync();

        // Assert
        entity.CreatedAt.Should().Be(originalCreatedAt); // CreatedAt should not change
        entity.ModifiedAt.Should().BeOnOrAfter(beforeCommit);
    }

    #endregion

    #region RollbackAsync Tests

    [Fact]
    public async Task RollbackAsync_ShouldSetIsRolledBack()
    {
        // Arrange
        using var uow = new UnitOfWork(_context, _serviceProvider);

        // Act
        await uow.RollbackAsync();

        // Assert
        uow.IsRolledBack.Should().BeTrue();
    }

    [Fact]
    public async Task RollbackAsync_ShouldDetachEntities()
    {
        // Arrange
        using var uow = new UnitOfWork(_context, _serviceProvider);
        var entity = new TestEntity { Name = "Test" };
        _context.Set<TestEntity>().Add(entity);
        _context.ChangeTracker.Entries().Should().NotBeEmpty();

        // Act
        await uow.RollbackAsync();

        // Assert
        _context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task RollbackAsync_Twice_ShouldNotThrow()
    {
        // Arrange
        using var uow = new UnitOfWork(_context, _serviceProvider);
        await uow.RollbackAsync();

        // Act & Assert - Should not throw
        await uow.RollbackAsync();
    }

    #endregion

    #region Transaction Tests

    [Fact]
    public async Task BeginTransactionAsync_ShouldReturnTransactionScope()
    {
        // Arrange
        using var uow = new UnitOfWork(_context, _serviceProvider);

        // Act
        using var scope = await uow.BeginTransactionAsync();

        // Assert
        scope.Should().NotBeNull();
        scope.IsActive.Should().BeTrue();
        scope.TransactionId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task BeginTransactionAsync_Twice_ShouldThrow()
    {
        // Arrange
        using var uow = new UnitOfWork(_context, _serviceProvider);
        await using var scope = await uow.BeginTransactionAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.BeginTransactionAsync());
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var uow = new UnitOfWork(_context, _serviceProvider);

        // Act & Assert - Should not throw
        uow.Dispose();
    }

    [Fact]
    public async Task DisposeAsync_ShouldNotThrow()
    {
        // Arrange
        var uow = new UnitOfWork(_context, _serviceProvider);

        // Act & Assert - Should not throw
        await uow.DisposeAsync();
    }

    #endregion

    #region Test Entities

    private class TestEntity : IEntity<int>
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class AuditableTestEntity : IEntity<int>, IAuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }

    #endregion
}

public class TransactionScopeTests
{
    [Fact]
    public async Task CommitAsync_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var services = new ServiceCollection();
        using var serviceProvider = services.BuildServiceProvider();
        using var uow = new UnitOfWork(context, serviceProvider);
        await using var scope = await uow.BeginTransactionAsync();

        // Act
        await scope.CommitAsync();

        // Assert
        scope.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CommitAsync_WhenNotActive_ShouldThrow()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var services = new ServiceCollection();
        using var serviceProvider = services.BuildServiceProvider();
        using var uow = new UnitOfWork(context, serviceProvider);
        await using var scope = await uow.BeginTransactionAsync();
        await scope.CommitAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => scope.CommitAsync());
    }

    [Fact]
    public async Task RollbackAsync_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var services = new ServiceCollection();
        using var serviceProvider = services.BuildServiceProvider();
        using var uow = new UnitOfWork(context, serviceProvider);
        await using var scope = await uow.BeginTransactionAsync();

        // Act
        await scope.RollbackAsync();

        // Assert
        scope.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task RollbackAsync_WhenNotActive_ShouldNotThrow()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var services = new ServiceCollection();
        using var serviceProvider = services.BuildServiceProvider();
        using var uow = new UnitOfWork(context, serviceProvider);
        await using var scope = await uow.BeginTransactionAsync();
        await scope.RollbackAsync();

        // Act & Assert - Should not throw
        await scope.RollbackAsync();
    }

    [Fact]
    public async Task Dispose_WhenActive_ShouldRollback()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var services = new ServiceCollection();
        using var serviceProvider = services.BuildServiceProvider();
        using var uow = new UnitOfWork(context, serviceProvider);
        var scope = await uow.BeginTransactionAsync();
        scope.IsActive.Should().BeTrue();

        // Act
        scope.Dispose();

        // Assert
        scope.IsActive.Should().BeFalse();
    }
}

public class UnitOfWorkResultTests
{
    [Fact]
    public void Succeeded_ShouldCreateSuccessResult()
    {
        // Act
        var result = UnitOfWorkResult.Succeeded(5);

        // Assert
        result.Success.Should().BeTrue();
        result.AffectedRows.Should().Be(5);
        result.Error.Should().BeNull();
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void Failed_ShouldCreateFailureResult()
    {
        // Arrange
        var exception = new Exception("Test error");

        // Act
        var result = UnitOfWorkResult.Failed("Error message", exception);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Error message");
        result.Exception.Should().Be(exception);
        result.AffectedRows.Should().Be(0);
    }

    [Fact]
    public void Failed_WithoutException_ShouldCreateFailureResult()
    {
        // Act
        var result = UnitOfWorkResult.Failed("Error message");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Error message");
        result.Exception.Should().BeNull();
    }
}
