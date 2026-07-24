using CapTap.Application.DTOs.Schedule;
using CapTap.Application.Exceptions;
using CapTap.Application.Interfaces;
using CapTap.Application.Services;
using CapTap.Application.Validators;
using CapTap.Domain.Entities;
using CapTap.Domain.Enums;
using CapTap.Domain.Exceptions;
using FluentAssertions;
using MedicationEntity = CapTap.Domain.Entities.Medication;

namespace CapTap.Application.Tests.Scheduling;

public sealed class AdherenceAndScheduleTests
{
    [Fact]
    public async Task OnceDailySchedule_AppearsOnToday()
    {
        var fixture = Fixture.Create();
        var userId = Guid.NewGuid();
        var med = fixture.AddMedication(userId, "Metformin");
        await fixture.ScheduleService.CreateScheduleAsync(
            userId,
            med.Id,
            new CreateScheduleRequest
            {
                Frequency = FrequencyType.OnceDaily,
                ScheduledTime = new TimeOnly(8, 0),
                DoseQuantity = 1
            });

        var doses = await fixture.Adherence.GetTodayDosesAsync(userId);

        doses.Should().ContainSingle();
        doses[0].MedicationName.Should().Be("Metformin");
        doses[0].ScheduledTime.Should().Be(new TimeOnly(8, 0));
    }

    [Fact]
    public async Task TwiceDaily_UsesTwoScheduleRows()
    {
        var fixture = Fixture.Create();
        var userId = Guid.NewGuid();
        var med = fixture.AddMedication(userId, "Metformin");

        await fixture.ScheduleService.CreateScheduleAsync(
            userId, med.Id,
            new CreateScheduleRequest { Frequency = FrequencyType.TwiceDaily, ScheduledTime = new TimeOnly(8, 0), DoseQuantity = 1 });
        await fixture.ScheduleService.CreateScheduleAsync(
            userId, med.Id,
            new CreateScheduleRequest { Frequency = FrequencyType.TwiceDaily, ScheduledTime = new TimeOnly(20, 0), DoseQuantity = 1 });

        var doses = await fixture.Adherence.GetTodayDosesAsync(userId);

        doses.Should().HaveCount(2);
        doses.Select(d => d.ScheduledTime).Should().BeEquivalentTo([new TimeOnly(8, 0), new TimeOnly(20, 0)]);
    }

    [Fact]
    public async Task MultipleMedications_AppearOnToday()
    {
        var fixture = Fixture.Create();
        var userId = Guid.NewGuid();
        var metformin = fixture.AddMedication(userId, "Metformin");
        var vitamin = fixture.AddMedication(userId, "Vitamin D");

        await fixture.ScheduleService.CreateScheduleAsync(
            userId, metformin.Id,
            new CreateScheduleRequest { ScheduledTime = new TimeOnly(8, 0), DoseQuantity = 1 });
        await fixture.ScheduleService.CreateScheduleAsync(
            userId, vitamin.Id,
            new CreateScheduleRequest { ScheduledTime = new TimeOnly(20, 0), DoseQuantity = 1 });

        var doses = await fixture.Adherence.GetTodayDosesAsync(userId);

        doses.Should().HaveCount(2);
        doses.Select(d => d.MedicationName).Should().BeEquivalentTo(["Metformin", "Vitamin D"]);
    }

    [Fact]
    public void CalculateStatus_Taken_WhenLogExists()
    {
        var fixture = Fixture.Create();
        var status = fixture.Adherence.CalculateStatus(
            new TimeOnly(8, 0),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTime.UtcNow,
            hasLog: true);

        status.Should().Be(AdherenceStatus.Taken);
    }

    [Fact]
    public void CalculateStatus_Upcoming_BeforeScheduledTime()
    {
        var fixture = Fixture.Create();
        var today = new DateOnly(2026, 7, 22);
        var now = new DateTime(2026, 7, 22, 7, 0, 0, DateTimeKind.Utc);

        var status = fixture.Adherence.CalculateStatus(new TimeOnly(8, 0), today, now, hasLog: false);

        status.Should().Be(AdherenceStatus.Upcoming);
    }

    [Fact]
    public void CalculateStatus_Due_AfterScheduledTimeSameDay()
    {
        var fixture = Fixture.Create();
        var today = new DateOnly(2026, 7, 22);
        var now = new DateTime(2026, 7, 22, 9, 0, 0, DateTimeKind.Utc);

        var status = fixture.Adherence.CalculateStatus(new TimeOnly(8, 0), today, now, hasLog: false);

        status.Should().Be(AdherenceStatus.Due);
    }

    [Fact]
    public void CalculateStatus_Missed_AfterCalendarDayEnds()
    {
        var fixture = Fixture.Create();
        var yesterday = new DateOnly(2026, 7, 21);
        var now = new DateTime(2026, 7, 22, 10, 0, 0, DateTimeKind.Utc);

        var status = fixture.Adherence.CalculateStatus(new TimeOnly(8, 0), yesterday, now, hasLog: false);

        status.Should().Be(AdherenceStatus.Missed);
    }

    [Fact]
    public async Task DuplicateScheduleTime_IsRejected()
    {
        var fixture = Fixture.Create();
        var userId = Guid.NewGuid();
        var med = fixture.AddMedication(userId, "Metformin");

        await fixture.ScheduleService.CreateScheduleAsync(
            userId, med.Id,
            new CreateScheduleRequest { ScheduledTime = new TimeOnly(8, 0), DoseQuantity = 1 });

        var act = () => fixture.ScheduleService.CreateScheduleAsync(
            userId, med.Id,
            new CreateScheduleRequest { ScheduledTime = new TimeOnly(8, 0), DoseQuantity = 1 });

        await act.Should().ThrowAsync<InvalidRequestException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task Ownership_OtherUserCannotAccessSchedule()
    {
        var fixture = Fixture.Create();
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var med = fixture.AddMedication(ownerId, "Metformin");

        var created = await fixture.ScheduleService.CreateScheduleAsync(
            ownerId, med.Id,
            new CreateScheduleRequest { ScheduledTime = new TimeOnly(8, 0), DoseQuantity = 1 });

        var act = () => fixture.ScheduleService.UpdateScheduleAsync(
            otherId,
            created.Id,
            new UpdateScheduleRequest { DoseQuantity = 2 });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task TodayDoses_WithLog_AreTakenAndSorted()
    {
        var clock = new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc);
        var fixture = Fixture.Create(clock);
        var userId = Guid.NewGuid();
        var med = fixture.AddMedication(userId, "Metformin");

        var morning = await fixture.ScheduleService.CreateScheduleAsync(
            userId, med.Id,
            new CreateScheduleRequest
            {
                ScheduledTime = new TimeOnly(8, 0),
                DoseQuantity = 1,
                EffectiveFrom = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        await fixture.ScheduleService.CreateScheduleAsync(
            userId, med.Id,
            new CreateScheduleRequest
            {
                ScheduledTime = new TimeOnly(20, 0),
                DoseQuantity = 1,
                EffectiveFrom = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)
            });

        fixture.Logs.Items.Add(new MedicationLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MedicationId = med.Id,
            ScheduleId = morning.Id,
            ScheduledDoseTime = new DateTime(2026, 7, 22, 8, 0, 0, DateTimeKind.Utc),
            LoggedAt = new DateTime(2026, 7, 22, 8, 5, 0, DateTimeKind.Utc),
            LoggingMethod = LoggingMethod.Manual
        });

        var doses = await fixture.Adherence.GetTodayDosesAsync(userId);

        doses.Should().HaveCount(2);
        // At 12:00 UTC: 20:00 Upcoming, 08:00 Taken → sort Due, Upcoming, Taken, Missed
        doses.Select(d => d.Status).Should().Equal(AdherenceStatus.Upcoming, AdherenceStatus.Taken);
        doses[1].LogId.Should().Be(fixture.Logs.Items[0].Id);
    }

    private sealed class Fixture
    {
        public required ScheduleService ScheduleService { get; init; }
        public required AdherenceService Adherence { get; init; }
        public required InMemoryMedicationRepository Medications { get; init; }
        public required InMemoryLogRepository Logs { get; init; }
        public required InMemoryUserRepository Users { get; init; }

        public static Fixture Create(DateTime? clock = null)
        {
            var medications = new InMemoryMedicationRepository();
            var schedules = new InMemoryScheduleRepository(medications);
            var logs = new InMemoryLogRepository();
            var time = new FixedTimeProvider(clock ?? DateTime.UtcNow);

            var scheduleService = new ScheduleService(
                schedules,
                medications,
                new FakeUnitOfWork(),
                new FakeAuditService(),
                new CreateScheduleRequestValidator(),
                new UpdateScheduleRequestValidator());

            var users = new InMemoryUserRepository();
            var adherence = new AdherenceService(schedules, logs, users, time);

            return new Fixture
            {
                ScheduleService = scheduleService,
                Adherence = adherence,
                Medications = medications,
                Logs = logs,
                Users = users
            };
        }

        public MedicationEntity AddMedication(Guid userId, string name)
        {
            Users.Ensure(userId);
            var medication = new MedicationEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = name,
                DosageAmount = 500,
                DosageUnit = "mg",
                IsArchived = false
            };
            Medications.Items.Add(medication);
            return medication;
        }
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        public Dictionary<Guid, User> Items { get; } = new();

        public void Ensure(Guid userId, string timeZoneId = "UTC")
        {
            if (!Items.ContainsKey(userId))
            {
                Items[userId] = new User
                {
                    Id = userId,
                    Email = $"{userId:N}@test.local",
                    PasswordHash = "x",
                    TimeZoneId = timeZoneId
                };
            }
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Values.FirstOrDefault(u => u.Email == email));

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.TryGetValue(id, out var user) ? user : null);

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            Items[user.Id] = user;
            return Task.CompletedTask;
        }

        public void Update(User user) => Items[user.Id] = user;
    }

    private sealed class FixedTimeProvider : ITimeProvider
    {
        public FixedTimeProvider(DateTime utcNow) => UtcNow = utcNow;

        public DateTime UtcNow { get; }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FakeAuditService : IAuditService
    {
        public Task LogAsync(
            string action,
            string entityType,
            Guid? userId = null,
            Guid? entityId = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default,
            string? metadata = null) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryMedicationRepository : IMedicationRepository
    {
        public List<MedicationEntity> Items { get; } = [];

        public Task<List<MedicationEntity>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Where(m => m.UserId == userId && !m.IsArchived).ToList());

        public Task<MedicationEntity?> GetByIdForUserAsync(Guid userId, Guid medicationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(m => m.Id == medicationId && m.UserId == userId));

        public Task AddAsync(MedicationEntity medication, CancellationToken cancellationToken = default)
        {
            Items.Add(medication);
            return Task.CompletedTask;
        }

        public void Update(MedicationEntity medication)
        {
        }

        public Task ArchiveAsync(Guid userId, Guid medicationId, CancellationToken cancellationToken = default)
        {
            var medication = Items.FirstOrDefault(m => m.Id == medicationId && m.UserId == userId);
            if (medication is not null)
            {
                medication.IsArchived = true;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryScheduleRepository : IScheduleRepository
    {
        private readonly InMemoryMedicationRepository _medications;
        public List<MedicationSchedule> Items { get; } = [];

        public InMemoryScheduleRepository(InMemoryMedicationRepository medications) => _medications = medications;

        public Task<List<MedicationSchedule>> GetByMedicationForUserAsync(
            Guid userId,
            Guid medicationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items
                .Where(s => s.MedicationId == medicationId &&
                            _medications.Items.Any(m => m.Id == s.MedicationId && m.UserId == userId))
                .OrderBy(s => s.ScheduledTime)
                .ToList());

        public Task<MedicationSchedule?> GetByIdForUserAsync(
            Guid userId,
            Guid scheduleId,
            CancellationToken cancellationToken = default)
        {
            var schedule = Items.FirstOrDefault(s => s.Id == scheduleId);
            if (schedule is null)
            {
                return Task.FromResult<MedicationSchedule?>(null);
            }

            var med = _medications.Items.FirstOrDefault(m => m.Id == schedule.MedicationId && m.UserId == userId);
            if (med is null)
            {
                return Task.FromResult<MedicationSchedule?>(null);
            }

            schedule.Medication = med;
            return Task.FromResult<MedicationSchedule?>(schedule);
        }

        public Task<List<MedicationSchedule>> GetActiveForUserOnDateAsync(
            Guid userId,
            DateOnly date,
            CancellationToken cancellationToken = default)
        {
            var dayStart = date.ToDateTime(TimeOnly.MinValue);
            var dayEnd = date.ToDateTime(TimeOnly.MaxValue);

            var result = Items
                .Where(s =>
                {
                    var med = _medications.Items.FirstOrDefault(m => m.Id == s.MedicationId && m.UserId == userId);
                    if (med is null || med.IsArchived || !s.IsActive)
                    {
                        return false;
                    }

                    if (s.EffectiveFrom.Date > dayEnd.Date)
                    {
                        return false;
                    }

                    if (s.EffectiveTo.HasValue && s.EffectiveTo.Value.Date < dayStart.Date)
                    {
                        return false;
                    }

                    s.Medication = med;
                    return true;
                })
                .OrderBy(s => s.ScheduledTime)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<bool> HasActiveDuplicateTimeAsync(
            Guid medicationId,
            TimeOnly scheduledTime,
            Guid? excludeScheduleId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Any(s =>
                s.MedicationId == medicationId &&
                s.IsActive &&
                s.ScheduledTime == scheduledTime &&
                (excludeScheduleId is null || s.Id != excludeScheduleId)));

        public Task AddAsync(MedicationSchedule schedule, CancellationToken cancellationToken = default)
        {
            if (schedule.Id == Guid.Empty)
            {
                schedule.Id = Guid.NewGuid();
            }

            var med = _medications.Items.First(m => m.Id == schedule.MedicationId);
            schedule.Medication = med;
            Items.Add(schedule);
            return Task.CompletedTask;
        }

        public void Update(MedicationSchedule schedule)
        {
        }

        public void Remove(MedicationSchedule schedule) => Items.Remove(schedule);
    }

    private sealed class InMemoryLogRepository : IMedicationLogRepository
    {
        public List<MedicationLog> Items { get; } = [];

        public Task<List<MedicationLog>> GetForUserOnDateAsync(
            Guid userId,
            DateOnly localDate,
            TimeZoneInfo timeZone,
            CancellationToken cancellationToken = default)
        {
            var start = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(localDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified),
                timeZone);
            var end = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(localDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified),
                timeZone);

            return Task.FromResult(Items
                .Where(l =>
                    l.UserId == userId &&
                    l.ScheduledDoseTime >= start &&
                    l.ScheduledDoseTime < end)
                .ToList());
        }

        public Task<List<MedicationLog>> GetForUserBetweenDatesAsync(
            Guid userId,
            DateOnly fromLocalDate,
            DateOnly toLocalDateInclusive,
            TimeZoneInfo timeZone,
            CancellationToken cancellationToken = default)
        {
            var start = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(fromLocalDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified),
                timeZone);
            var end = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(toLocalDateInclusive.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified),
                timeZone);

            return Task.FromResult(Items
                .Where(l =>
                    l.UserId == userId &&
                    l.ScheduledDoseTime >= start &&
                    l.ScheduledDoseTime < end)
                .ToList());
        }

        public Task<MedicationLog?> FindDuplicateAsync(
            Guid userId,
            Guid scheduleId,
            DateTime scheduledDoseTime,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(l =>
                l.UserId == userId &&
                l.ScheduleId == scheduleId &&
                l.ScheduledDoseTime == scheduledDoseTime));

        public Task<(List<MedicationLog> Items, int TotalCount)> GetHistoryAsync(
            Guid userId,
            int page,
            int pageSize,
            Guid? medicationId = null,
            CancellationToken cancellationToken = default)
        {
            var query = Items.Where(l => l.UserId == userId);
            if (medicationId.HasValue)
            {
                query = query.Where(l => l.MedicationId == medicationId.Value);
            }

            var list = query
                .OrderByDescending(l => l.LoggedAt)
                .ToList();
            var pageItems = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult((pageItems, list.Count));
        }

        public Task AddAsync(MedicationLog log, CancellationToken cancellationToken = default)
        {
            if (log.Id == Guid.Empty)
            {
                log.Id = Guid.NewGuid();
            }

            Items.Add(log);
            return Task.CompletedTask;
        }
    }
}
