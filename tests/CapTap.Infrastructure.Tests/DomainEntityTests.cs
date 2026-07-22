using CapTap.Domain.Entities;
using CapTap.Domain.Enums;
using CapTap.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CapTap.Infrastructure.Tests;

public class DomainEntityTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("captap_domain_test")
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
    public async Task DatabaseContext_Should_Initialize_And_Migrate()
    {
        await using var context = CreateContext();

        await context.Database.MigrateAsync();

        var canConnect = await context.Database.CanConnectAsync();
        canConnect.Should().BeTrue();

        (await context.Users.AnyAsync()).Should().BeFalse();
        (await context.Medications.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task User_Should_Be_Inserted()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var user = new User
        {
            Email = "demo@captap.com",
            PasswordHash = "hashed-password",
            EmailVerified = false,
            IsActive = true
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var stored = await context.Users.SingleAsync(u => u.Email == "demo@captap.com");
        stored.Id.Should().NotBeEmpty();
        stored.EmailVerified.Should().BeFalse();
        stored.IsActive.Should().BeTrue();
        stored.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Medication_Relationship_Should_Work_With_Schedule()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var user = new User
        {
            Email = "patient@captap.com",
            PasswordHash = "hashed-password"
        };

        var medication = new Medication
        {
            User = user,
            Name = "Metformin",
            GenericName = "Metformin",
            DosageAmount = 500,
            DosageUnit = "mg",
            Form = "Tablet"
        };

        var schedule = new MedicationSchedule
        {
            Medication = medication,
            Frequency = FrequencyType.TwiceDaily,
            DoseQuantity = 1,
            ScheduledTime = new TimeOnly(8, 0),
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow.Date
        };

        context.Users.Add(user);
        context.Medications.Add(medication);
        context.MedicationSchedules.Add(schedule);
        await context.SaveChangesAsync();

        var stored = await context.Medications
            .Include(m => m.User)
            .Include(m => m.Schedules)
            .SingleAsync(m => m.Name == "Metformin");

        stored.User.Email.Should().Be("patient@captap.com");
        stored.Schedules.Should().HaveCount(1);
        stored.Schedules.First().Frequency.Should().Be(FrequencyType.TwiceDaily);
        stored.Schedules.First().ScheduledTime.Should().Be(new TimeOnly(8, 0));
    }

    [Fact]
    public async Task NfcTag_Uniqueness_Constraint_Should_Reject_Duplicates()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var user = new User
        {
            Email = "nfc@captap.com",
            PasswordHash = "hashed-password"
        };

        var firstMedication = new Medication
        {
            User = user,
            Name = "Vitamin D",
            DosageAmount = 1000,
            DosageUnit = "IU"
        };

        var secondMedication = new Medication
        {
            User = user,
            Name = "Lisinopril",
            DosageAmount = 10,
            DosageUnit = "mg"
        };

        context.Users.Add(user);
        context.Medications.AddRange(firstMedication, secondMedication);
        await context.SaveChangesAsync();

        context.NfcTags.Add(new NfcTag
        {
            MedicationId = firstMedication.Id,
            TagIdentifier = "04:A2:9F:77",
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        context.NfcTags.Add(new NfcTag
        {
            MedicationId = secondMedication.Id,
            TagIdentifier = "04:A2:9F:77",
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });

        var act = async () => await context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Frequency_And_LoggingMethod_Should_Persist_As_Strings()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var user = new User
        {
            Email = "enums@captap.com",
            PasswordHash = "hashed-password"
        };

        var medication = new Medication
        {
            User = user,
            Name = "Vitamin D",
            DosageAmount = 1000,
            DosageUnit = "IU"
        };

        var schedule = new MedicationSchedule
        {
            Medication = medication,
            Frequency = FrequencyType.OnceDaily,
            DoseQuantity = 1,
            ScheduledTime = new TimeOnly(9, 0),
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow.Date
        };

        var log = new MedicationLog
        {
            User = user,
            Medication = medication,
            Schedule = schedule,
            TakenAt = DateTime.UtcNow,
            LoggingMethod = LoggingMethod.Nfc
        };

        context.AddRange(user, medication, schedule, log);
        await context.SaveChangesAsync();

        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();

        await using var frequencyCommand = new NpgsqlCommand(
            """SELECT "Frequency" FROM "MedicationSchedules" LIMIT 1""",
            connection);
        var frequency = (string?)await frequencyCommand.ExecuteScalarAsync();
        frequency.Should().Be("OnceDaily");

        await using var methodCommand = new NpgsqlCommand(
            """SELECT "LoggingMethod" FROM "MedicationLogs" LIMIT 1""",
            connection);
        var method = (string?)await methodCommand.ExecuteScalarAsync();
        method.Should().Be("Nfc");
    }

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
