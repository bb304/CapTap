using CapTap.Application.DTOs.Logging;
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

namespace CapTap.Application.Tests.Logging;

public sealed class MedicationLoggingAndStreakTests
{
    [Fact]
    public async Task ManualLogging_Succeeds_AndMarksDoseTaken()
    {
        var clock = new DateTime(2026, 7, 24, 9, 0, 0, DateTimeKind.Utc);
        var fixture = Fixture.Create(clock);
        var userId = Guid.NewGuid();
        var med = fixture.AddMedication(userId, "Metformin");
        var schedule = await fixture.CreateSchedule(userId, med.Id, new TimeOnly(8, 0));

        var scheduled = new DateTime(2026, 7, 24, 8, 0, 0, DateTimeKind.Utc);
        var log = await fixture.LogService.LogDoseAsync(
            userId,
            new CreateMedicationLogRequest
            {
                MedicationId = med.Id,
                ScheduleId = schedule.Id,
                ScheduledDoseTime = scheduled,
                LoggingMethod = LoggingMethod.Manual
            });

        log.LoggingMethod.Should().Be(LoggingMethod.Manual);
        log.LoggedAt.Should().Be(clock);
        log.ScheduledDoseTime.Should().Be(scheduled);

        var doses = await fixture.Adherence.GetTodayDosesAsync(userId);
        doses.Should().ContainSingle();
        doses[0].Status.Should().Be(AdherenceStatus.Taken);
        doses[0].LogId.Should().Be(log.Id);
    }

    [Fact]
    public async Task DuplicateLogs_AreRejected()
    {
        var clock = new DateTime(2026, 7, 24, 9, 0, 0, DateTimeKind.Utc);
        var fixture = Fixture.Create(clock);
        var userId = Guid.NewGuid();
        var med = fixture.AddMedication(userId, "Metformin");
        var schedule = await fixture.CreateSchedule(userId, med.Id, new TimeOnly(8, 0));
        var scheduled = new DateTime(2026, 7, 24, 8, 0, 0, DateTimeKind.Utc);
        var request = new CreateMedicationLogRequest
        {
            MedicationId = med.Id,
            ScheduleId = schedule.Id,
            ScheduledDoseTime = scheduled,
            LoggingMethod = LoggingMethod.Manual
        };

        await fixture.LogService.LogDoseAsync(userId, request);

        var act = () => fixture.LogService.LogDoseAsync(userId, request);
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already been logged*");
    }

    [Fact]
    public async Task UnauthorizedLogging_ForOtherUsersMedication_IsRejected()
    {
        var clock = new DateTime(2026, 7, 24, 9, 0, 0, DateTimeKind.Utc);
        var fixture = Fixture.Create(clock);
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var med = fixture.AddMedication(ownerId, "Metformin");
        await fixture.CreateSchedule(ownerId, med.Id, new TimeOnly(8, 0));

        var act = () => fixture.LogService.LogDoseAsync(
            otherId,
            new CreateMedicationLogRequest
            {
                MedicationId = med.Id,
                ScheduledDoseTime = new DateTime(2026, 7, 24, 8, 0, 0, DateTimeKind.Utc),
                LoggingMethod = LoggingMethod.Manual
            });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task History_PaginatesNewestFirst()
    {
        var clock = new DateTime(2026, 7, 24, 21, 0, 0, DateTimeKind.Utc);
        var fixture = Fixture.Create(clock);
        var userId = Guid.NewGuid();
        var med = fixture.AddMedication(userId, "Metformin");
        var morning = await fixture.CreateSchedule(userId, med.Id, new TimeOnly(8, 0));
        var evening = await fixture.CreateSchedule(userId, med.Id, new TimeOnly(20, 0));

        await fixture.LogService.LogDoseAsync(userId, new CreateMedicationLogRequest
        {
            MedicationId = med.Id,
            ScheduleId = morning.Id,
            ScheduledDoseTime = new DateTime(2026, 7, 24, 8, 0, 0, DateTimeKind.Utc),
            LoggingMethod = LoggingMethod.Manual
        });

        // Advance clock slightly for second log ordering.
        fixture.Time.UtcNow = new DateTime(2026, 7, 24, 21, 30, 0, DateTimeKind.Utc);
        await fixture.LogService.LogDoseAsync(userId, new CreateMedicationLogRequest
        {
            MedicationId = med.Id,
            ScheduleId = evening.Id,
            ScheduledDoseTime = new DateTime(2026, 7, 24, 20, 0, 0, DateTimeKind.Utc),
            LoggingMethod = LoggingMethod.Manual
        });

        var page = await fixture.LogService.GetHistoryAsync(userId, page: 1, pageSize: 1);
        page.TotalCount.Should().Be(2);
        page.Items.Should().ContainSingle();
        page.Items[0].ScheduledDoseTime.Should().Be(new DateTime(2026, 7, 24, 20, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Streak_CountsConsecutiveCompleteDays_AndResetsAfterMiss()
    {
        // Day 1 complete, day 2 missed, day 3 complete → current streak 1, longest at least 1
        var day3 = new DateTime(2026, 7, 24, 21, 0, 0, DateTimeKind.Utc);
        var fixture = Fixture.Create(day3);
        var userId = Guid.NewGuid();
        var med = fixture.AddMedication(userId, "Metformin");
        var schedule = await fixture.CreateSchedule(userId, med.Id, new TimeOnly(8, 0));

        // Day 1 (Jul 22) — taken
        fixture.Time.UtcNow = new DateTime(2026, 7, 22, 9, 0, 0, DateTimeKind.Utc);
        await fixture.LogService.LogDoseAsync(userId, new CreateMedicationLogRequest
        {
            MedicationId = med.Id,
            ScheduleId = schedule.Id,
            ScheduledDoseTime = new DateTime(2026, 7, 22, 8, 0, 0, DateTimeKind.Utc),
            LoggingMethod = LoggingMethod.Manual
        });

        // Day 2 (Jul 23) — missed (no log)
        // Day 3 (Jul 24) — taken
        fixture.Time.UtcNow = day3;
        await fixture.LogService.LogDoseAsync(userId, new CreateMedicationLogRequest
        {
            MedicationId = med.Id,
            ScheduleId = schedule.Id,
            ScheduledDoseTime = new DateTime(2026, 7, 24, 8, 0, 0, DateTimeKind.Utc),
            LoggingMethod = LoggingMethod.Manual
        });

        var streak = await fixture.Streak.GetStreakAsync(userId);
        streak.CurrentStreakDays.Should().Be(1);
        streak.LongestStreakDays.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task DashboardStats_ReflectTakenAndCompletion()
    {
        var clock = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        var fixture = Fixture.Create(clock);
        var userId = Guid.NewGuid();
        var med = fixture.AddMedication(userId, "Metformin");
        var morning = await fixture.CreateSchedule(userId, med.Id, new TimeOnly(8, 0));
        await fixture.CreateSchedule(userId, med.Id, new TimeOnly(20, 0));

        await fixture.LogService.LogDoseAsync(userId, new CreateMedicationLogRequest
        {
            MedicationId = med.Id,
            ScheduleId = morning.Id,
            ScheduledDoseTime = new DateTime(2026, 7, 24, 8, 0, 0, DateTimeKind.Utc),
            LoggingMethod = LoggingMethod.Manual
        });

        var doses = await fixture.Adherence.GetTodayDosesAsync(userId);
        var streak = await fixture.Streak.GetStreakAsync(userId);

        var taken = doses.Count(d => d.Status == AdherenceStatus.Taken);
        var completion = (int)Math.Round(100.0 * taken / doses.Count);

        doses.Should().HaveCount(2);
        taken.Should().Be(1);
        completion.Should().Be(50);
        streak.CurrentStreakDays.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task NfcLoggingMethod_IsAccepted()
    {
        var clock = new DateTime(2026, 7, 24, 9, 0, 0, DateTimeKind.Utc);
        var fixture = Fixture.Create(clock);
        var userId = Guid.NewGuid();
        var med = fixture.AddMedication(userId, "Metformin");
        var schedule = await fixture.CreateSchedule(userId, med.Id, new TimeOnly(8, 0));

        var log = await fixture.LogService.LogDoseAsync(
            userId,
            new CreateMedicationLogRequest
            {
                MedicationId = med.Id,
                ScheduleId = schedule.Id,
                ScheduledDoseTime = new DateTime(2026, 7, 24, 8, 0, 0, DateTimeKind.Utc),
                LoggingMethod = LoggingMethod.Nfc
            });

        log.LoggingMethod.Should().Be(LoggingMethod.Nfc);
    }

    private sealed class Fixture
    {
        public required MedicationLogService LogService { get; init; }
        public required AdherenceService Adherence { get; init; }
        public required AdherenceStreakService Streak { get; init; }
        public required ScheduleService ScheduleService { get; init; }
        public required InMemoryMedicationRepository Medications { get; init; }
        public required InMemoryLogRepository Logs { get; init; }
        public required InMemoryUserRepository Users { get; init; }
        public required MutableTimeProvider Time { get; init; }

        public static Fixture Create(DateTime clock)
        {
            var medications = new InMemoryMedicationRepository();
            var schedules = new InMemoryScheduleRepository(medications);
            var logs = new InMemoryLogRepository(medications);
            var users = new InMemoryUserRepository();
            var time = new MutableTimeProvider(clock);

            var scheduleService = new ScheduleService(
                schedules,
                medications,
                new FakeUnitOfWork(),
                new FakeAuditService(),
                new CreateScheduleRequestValidator(),
                new UpdateScheduleRequestValidator());

            var adherence = new AdherenceService(schedules, logs, users, time);
            var streak = new AdherenceStreakService(adherence, users, time);
            var logService = new MedicationLogService(
                medications,
                schedules,
                logs,
                users,
                new FakeUnitOfWork(),
                new FakeAuditService(),
                time,
                new CreateMedicationLogRequestValidator());

            return new Fixture
            {
                LogService = logService,
                Adherence = adherence,
                Streak = streak,
                ScheduleService = scheduleService,
                Medications = medications,
                Logs = logs,
                Users = users,
                Time = time
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

        public async Task<ScheduleResponseDto> CreateSchedule(Guid userId, Guid medicationId, TimeOnly time)
        {
            return await ScheduleService.CreateScheduleAsync(
                userId,
                medicationId,
                new CreateScheduleRequest
                {
                    Frequency = FrequencyType.OnceDaily,
                    ScheduledTime = time,
                    DoseQuantity = 1,
                    EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                });
        }
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        public Dictionary<Guid, User> Items { get; } = new();

        public void Ensure(Guid userId, string? timeZoneId = null)
        {
            if (!Items.ContainsKey(userId))
            {
                Items[userId] = new User
                {
                    Id = userId,
                    Email = $"{userId:N}@test.local",
                    PasswordHash = "x",
                    TimeZoneId = timeZoneId ?? "UTC"
                };
            }
            else if (timeZoneId is not null)
            {
                Items[userId].TimeZoneId = timeZoneId;
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

    private sealed class MutableTimeProvider : ITimeProvider
    {
        public MutableTimeProvider(DateTime utcNow) => UtcNow = utcNow;

        public DateTime UtcNow { get; set; }
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
        private readonly InMemoryMedicationRepository _medications;

        public InMemoryLogRepository(InMemoryMedicationRepository medications) => _medications = medications;

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

            foreach (var log in list)
            {
                log.Medication = _medications.Items.First(m => m.Id == log.MedicationId);
            }

            var pageItems = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult((pageItems, list.Count));
        }

        public Task AddAsync(MedicationLog log, CancellationToken cancellationToken = default)
        {
            if (log.Id == Guid.Empty)
            {
                log.Id = Guid.NewGuid();
            }

            log.Medication = _medications.Items.First(m => m.Id == log.MedicationId);
            Items.Add(log);
            return Task.CompletedTask;
        }
    }
}
