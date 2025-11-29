using System.Linq.Expressions;
using Arcana.Core.Common;
using Arcana.Data.Local;
using Arcana.Data.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Arcana.Sync.Tests.Repository;

public class RepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Repository<TestEntity, int> _repository;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new Repository<TestEntity, int>(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ShouldReturnEntity()
    {
        // Arrange
        var entity = new TestEntity { Name = "Test" };
        _context.Set<TestEntity>().Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingEntity_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_EmptyRepository_ShouldReturnEmptyList()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithEntities_ShouldReturnAll()
    {
        // Arrange
        _context.Set<TestEntity>().AddRange(
            new TestEntity { Name = "Test1" },
            new TestEntity { Name = "Test2" },
            new TestEntity { Name = "Test3" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    #endregion

    #region FindAsync Tests

    [Fact]
    public async Task FindAsync_MatchingPredicate_ShouldReturnMatches()
    {
        // Arrange
        _context.Set<TestEntity>().AddRange(
            new TestEntity { Name = "Alpha" },
            new TestEntity { Name = "Beta" },
            new TestEntity { Name = "Alpha2" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FindAsync(e => e.Name.StartsWith("Alpha"));

        // Assert
        result.Should().HaveCount(2);
        result.All(e => e.Name.StartsWith("Alpha")).Should().BeTrue();
    }

    [Fact]
    public async Task FindAsync_NoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        _context.Set<TestEntity>().Add(new TestEntity { Name = "Test" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FindAsync(e => e.Name == "NonExistent");

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region FirstOrDefaultAsync Tests

    [Fact]
    public async Task FirstOrDefaultAsync_MatchingPredicate_ShouldReturnFirst()
    {
        // Arrange
        _context.Set<TestEntity>().AddRange(
            new TestEntity { Name = "First" },
            new TestEntity { Name = "Second" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FirstOrDefaultAsync(e => e.Name == "First");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("First");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_NoMatch_ShouldReturnNull()
    {
        // Arrange
        _context.Set<TestEntity>().Add(new TestEntity { Name = "Test" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FirstOrDefaultAsync(e => e.Name == "NonExistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region AnyAsync Tests

    [Fact]
    public async Task AnyAsync_MatchingPredicate_ShouldReturnTrue()
    {
        // Arrange
        _context.Set<TestEntity>().Add(new TestEntity { Name = "Test" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.AnyAsync(e => e.Name == "Test");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AnyAsync_NoMatch_ShouldReturnFalse()
    {
        // Arrange
        _context.Set<TestEntity>().Add(new TestEntity { Name = "Test" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.AnyAsync(e => e.Name == "NonExistent");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_WithoutPredicate_ShouldReturnTotalCount()
    {
        // Arrange
        _context.Set<TestEntity>().AddRange(
            new TestEntity { Name = "Test1" },
            new TestEntity { Name = "Test2" },
            new TestEntity { Name = "Test3" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task CountAsync_WithPredicate_ShouldReturnMatchingCount()
    {
        // Arrange
        _context.Set<TestEntity>().AddRange(
            new TestEntity { Name = "Alpha" },
            new TestEntity { Name = "Beta" },
            new TestEntity { Name = "Alpha2" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CountAsync(e => e.Name.StartsWith("Alpha"));

        // Assert
        result.Should().Be(2);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ShouldAddEntity()
    {
        // Arrange
        var entity = new TestEntity { Name = "New" };

        // Act
        var result = await _repository.AddAsync(entity);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);

        var retrieved = await _repository.GetByIdAsync(result.Id);
        retrieved.Should().NotBeNull();
    }

    [Fact]
    public async Task AddAsync_SyncableEntity_ShouldSetPendingSync()
    {
        // Arrange
        var repo = new Repository<SyncableTestEntity, int>(_context);
        var entity = new SyncableTestEntity { Name = "Syncable" };

        // Act
        var result = await repo.AddAsync(entity);

        // Assert
        result.IsPendingSync.Should().BeTrue();
    }

    #endregion

    #region AddRangeAsync Tests

    [Fact]
    public async Task AddRangeAsync_ShouldAddAllEntities()
    {
        // Arrange
        var entities = new[]
        {
            new TestEntity { Name = "Entity1" },
            new TestEntity { Name = "Entity2" },
            new TestEntity { Name = "Entity3" }
        };

        // Act
        await _repository.AddRangeAsync(entities);

        // Assert
        var count = await _repository.CountAsync();
        count.Should().Be(3);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ShouldUpdateEntity()
    {
        // Arrange
        var entity = new TestEntity { Name = "Original" };
        _context.Set<TestEntity>().Add(entity);
        await _context.SaveChangesAsync();
        _context.Entry(entity).State = EntityState.Detached;

        entity.Name = "Updated";

        // Act
        await _repository.UpdateAsync(entity);

        // Assert
        var retrieved = await _repository.GetByIdAsync(entity.Id);
        retrieved!.Name.Should().Be("Updated");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_RegularEntity_ShouldRemove()
    {
        // Arrange
        var entity = new TestEntity { Name = "ToDelete" };
        _context.Set<TestEntity>().Add(entity);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(entity);

        // Assert
        var retrieved = await _repository.GetByIdAsync(entity.Id);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletableEntity_ShouldSoftDelete()
    {
        // Arrange
        var repo = new Repository<SoftDeletableTestEntity, int>(_context);
        var entity = new SoftDeletableTestEntity { Name = "ToSoftDelete" };
        _context.Set<SoftDeletableTestEntity>().Add(entity);
        await _context.SaveChangesAsync();

        // Act
        await repo.DeleteAsync(entity);

        // Assert
        var retrieved = await repo.GetByIdAsync(entity.Id);
        retrieved.Should().NotBeNull();
        retrieved!.IsDeleted.Should().BeTrue();
        retrieved.DeletedAt.Should().NotBeNull();
    }

    #endregion

    #region DeleteByIdAsync Tests

    [Fact]
    public async Task DeleteByIdAsync_ExistingEntity_ShouldDelete()
    {
        // Arrange
        var entity = new TestEntity { Name = "ToDelete" };
        _context.Set<TestEntity>().Add(entity);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteByIdAsync(entity.Id);

        // Assert
        var retrieved = await _repository.GetByIdAsync(entity.Id);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task DeleteByIdAsync_NonExistingEntity_ShouldNotThrow()
    {
        // Act & Assert - Should not throw
        await _repository.DeleteByIdAsync(999);
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task Query_ShouldReturnQueryable()
    {
        // Arrange
        _context.Set<TestEntity>().AddRange(
            new TestEntity { Name = "Test1" },
            new TestEntity { Name = "Test2" }
        );
        await _context.SaveChangesAsync();

        // Act
        var query = _repository.Query();
        var result = await query.Where(e => e.Name == "Test1").ToListAsync();

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region GetPagedAsync Tests

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResult()
    {
        // Arrange
        for (int i = 1; i <= 25; i++)
        {
            _context.Set<TestEntity>().Add(new TestEntity { Name = $"Test{i}" });
        }
        await _context.SaveChangesAsync();

        var request = new PageRequest { Page = 2, PageSize = 10 };

        // Act
        var result = await _repository.GetPagedAsync(request);

        // Assert
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetPagedAsync_LastPage_ShouldReturnRemainingItems()
    {
        // Arrange
        for (int i = 1; i <= 25; i++)
        {
            _context.Set<TestEntity>().Add(new TestEntity { Name = $"Test{i}" });
        }
        await _context.SaveChangesAsync();

        var request = new PageRequest { Page = 3, PageSize = 10 };

        // Act
        var result = await _repository.GetPagedAsync(request);

        // Assert
        result.Items.Should().HaveCount(5);
    }

    #endregion

    #region Test Entities

    public class TestEntity : IEntity<int>
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class SyncableTestEntity : IEntity<int>, ISyncable
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid SyncId { get; set; }
        public DateTime? LastSyncAt { get; set; }
        public bool IsPendingSync { get; set; }
    }

    public class SoftDeletableTestEntity : IEntity<int>, ISoftDeletable
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }

    #endregion
}
