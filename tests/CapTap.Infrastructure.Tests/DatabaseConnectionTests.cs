using CapTap.Application.Interfaces;
using CapTap.Domain.Common;
using CapTap.Infrastructure.Persistence;
using CapTap.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CapTap.Infrastructure.Tests;

public class ProbeEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}

public class ProbeDbContext : ApplicationDbContext
{
    public ProbeDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProbeEntity> Probes => Set<ProbeEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProbeEntity>(entity =>
        {
            entity.ToTable("probe_entities");
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });
    }
}

public class DatabaseConnectionTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("captap_test")
        .WithUsername("captap")
        .WithPassword("captappassword")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Database_Should_Connect()
    {
        await using var context = CreateContext();

        var canConnect = await context.Database.CanConnectAsync();

        canConnect.Should().BeTrue();
    }

    [Fact]
    public async Task Repository_Should_Create_And_Read_Entity()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();

        IGenericRepository<ProbeEntity> repository = new GenericRepository<ProbeEntity>(context);
        IUnitOfWork unitOfWork = new UnitOfWork(context);

        var entity = new ProbeEntity { Name = "phase-1-probe" };
        await repository.AddAsync(entity);
        await unitOfWork.SaveChangesAsync();

        var stored = await repository.GetByIdAsync(entity.Id);

        stored.Should().NotBeNull();
        stored!.Name.Should().Be("phase-1-probe");
        stored.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        stored.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    private ProbeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new ProbeDbContext(options);
    }
}
