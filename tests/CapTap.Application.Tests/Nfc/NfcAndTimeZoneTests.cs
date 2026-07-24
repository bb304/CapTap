using CapTap.Application.Common;
using CapTap.Application.DTOs.Logging;
using CapTap.Application.DTOs.Nfc;
using CapTap.Application.DTOs.Schedule;
using CapTap.Application.Interfaces;
using CapTap.Application.Services;
using CapTap.Application.Validators;
using CapTap.Domain.Entities;
using CapTap.Domain.Enums;
using CapTap.Domain.Exceptions;
using FluentAssertions;
using MedicationEntity = CapTap.Domain.Entities.Medication;

namespace CapTap.Application.Tests.Nfc;

public sealed class NfcAndTimeZoneTests
{
    [Fact]
    public async Task Assign_And_Resolve_ReturnsTodaysDose()
    {
        var clock = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        var fixture = Fixture.Create(clock);
        var userId = Guid.NewGuid();
        var med = fixture.AddMedication(userId, "Metformin");
        var schedule = await fixture.CreateSchedule(userId, med.Id, new TimeOnly(8, 0));

        var assigned = await fixture.Nfc.AssignAsync(
            userId,
            new AssignNfcTagRequest { MedicationId = med.Id, TagIdentifier = "04:a8:92:aa" });

        assigned.IsAssigned.Should().BeTrue();
        assigned.TagIdentifier.Should().Be("04:A8:92:AA");

        var resolved = await fixture.Nfc.ResolveAsync(userId, "04:a8:92:aa");
        resolved.MedicationName.Should().Be("Metformin");
        resolved.ScheduleId.Should().Be(schedule.Id);
        resolved.AlreadyLogged.Should().BeFalse();
        resolved.ScheduledDoseTime.Should().Be(new DateTime(2026, 7, 24, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Resolve_UnknownTag_IsNotFound()
    {
        var fixture = Fixture.Create(new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc));
        var act = () => fixture.Nfc.ResolveAsync(Guid.NewGuid(), "NOPE");
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Resolve_OtherUsersTag_IsNotFound()
    {
        var clock = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        var fixture = Fixture.Create(clock);
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        var med = fixture.AddMedication(owner, "Metformin");
        await fixture.CreateSchedule(owner, med.Id, new TimeOnly(8, 0));
        await fixture.Nfc.AssignAsync(owner, new AssignNfcTagRequest
        {
            MedicationId = med.Id,
            TagIdentifier = "04:AA"
        });

        var act = () => fixture.Nfc.ResolveAsync(other, "04:AA");
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Reassign_UnassignsPreviousMedicationSafely()
    {
        var clock = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        var fixture = Fixture.Create(clock);
        var userId = Guid.NewGuid();
        var medA = fixture.AddMedication(userId, "A");
        var medB = fixture.AddMedication(userId, "B");
        await fixture.CreateSchedule(userId, medA.Id, new TimeOnly(8, 0));
        await fixture.CreateSchedule(userId, medB.Id, new TimeOnly(9, 0));

        await fixture.Nfc.AssignAsync(userId, new AssignNfcTagRequest
        {
            MedicationId = medA.Id,
            TagIdentifier = "TAG-1"
        });
        await fixture.Nfc.AssignAsync(userId, new AssignNfcTagRequest
        {
            MedicationId = medB.Id,
            TagIdentifier = "TAG-1"
        });

        var resolved = await fixture.Nfc.ResolveAsync(userId, "TAG-1");
        resolved.MedicationId.Should().Be(medB.Id);

        fixture.Tags.Items.Should().ContainSingle(t => t.TagIdentifier == "TAG-1" && t.IsAssigned);
        fixture.Tags.Items.Single(t => t.TagIdentifier == "TAG-1").MedicationId.Should().Be(medB.Id);
    }

    [Fact]
    public async Task Unassign_SoftDisablesTag()
    {
        var clock = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        var fixture = Fixture.Create(clock);
        var userId = Guid.NewGuid();
        var med = fixture.AddMedication(userId, "Metformin");
        await fixture.CreateSchedule(userId, med.Id, new TimeOnly(8, 0));
        await fixture.Nfc.AssignAsync(userId, new AssignNfcTagRequest
        {
            MedicationId = med.Id,
            TagIdentifier = "TAG-X"
        });

        await fixture.Nfc.UnassignAsync(userId, new UnassignNfcTagRequest { MedicationId = med.Id });

        fixture.Tags.Items.Should().ContainSingle(t => t.TagIdentifier == "TAG-X" && !t.IsAssigned);
        var act = () => fixture.Nfc.ResolveAsync(userId, "TAG-X");
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task NfcLogging_UsesSharedMedicationLogEndpoint()
    {
        var clock = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        var fixture = Fixture.Create(clock);
        var userId = Guid.NewGuid();
        var med = fixture.AddMedication(userId, "Metformin");
        var schedule = await fixture.CreateSchedule(userId, med.Id, new TimeOnly(8, 0));
        await fixture.Nfc.AssignAsync(userId, new AssignNfcTagRequest
        {
            MedicationId = med.Id,
            TagIdentifier = "TAG-L"
        });
        var resolved = await fixture.Nfc.ResolveAsync(userId, "TAG-L");

        var log = await fixture.LogService.LogDoseAsync(userId, new CreateMedicationLogRequest
        {
            MedicationId = resolved.MedicationId,
            ScheduleId = resolved.ScheduleId,
            ScheduledDoseTime = resolved.ScheduledDoseTime!.Value,
            LoggingMethod = LoggingMethod.Nfc
        });

        log.LoggingMethod.Should().Be(LoggingMethod.Nfc);
        log.ScheduleId.Should().Be(schedule.Id);
        fixture.Audit.Actions.Should().Contain("MEDICATION_LOGGED_NFC");
        fixture.Audit.Actions.Should().Contain("NFC_SCANNED");
        fixture.Audit.Actions.Should().Contain("NFC_ASSIGNED");
    }

    [Fact]
    public async Task TimeZone_LocalMidnightCrossing_UsesUserCalendarDay()
    {
        // 02:00 UTC on Jul 25 = 22:00 Jul 24 in America/New_York (EDT, UTC-4).
        var clock = new DateTime(2026, 7, 25, 2, 0, 0, DateTimeKind.Utc);
        var fixture = Fixture.Create(clock);
        var userId = Guid.NewGuid();
        fixture.Users.Ensure(userId, "America/New_York");
        var med = fixture.AddMedication(userId, "Metformin");
        await fixture.CreateSchedule(userId, med.Id, new TimeOnly(8, 0));

        var doses = await fixture.Adherence.GetTodayDosesAsync(userId);
        doses.Should().ContainSingle();
        // Local "today" is still Jul 24 → 08:00 local is Due (22:00 > 08:00).
        doses[0].Status.Should().Be(AdherenceStatus.Due);
        doses[0].ScheduledDoseTime.Should().Be(
            TimeZoneHelper.ToUtc(new DateOnly(2026, 7, 24), new TimeOnly(8, 0), TimeZoneHelper.Resolve("America/New_York")));
    }

    [Fact]
    public void TimeZoneHelper_RejectsInvalidIana()
    {
        TimeZoneHelper.IsValidIana("Not/AZone").Should().BeFalse();
        TimeZoneHelper.IsValidIana("America/New_York").Should().BeTrue();
    }

    private sealed class Fixture
    {
        public required NfcService Nfc { get; init; }
        public required MedicationLogService LogService { get; init; }
        public required AdherenceService Adherence { get; init; }
        public required InMemoryUserRepository Users { get; init; }
        public required InMemoryNfcTagRepository Tags { get; init; }
        public required RecordingAuditService Audit { get; init; }

        public static Fixture Create(DateTime clock)
        {
            var medications = new InMemoryMedicationRepository();
            var schedules = new InMemoryScheduleRepository(medications);
            var logs = new InMemoryLogRepository(medications);
            var users = new InMemoryUserRepository();
            var tags = new InMemoryNfcTagRepository(medications);
            var time = new MutableTimeProvider(clock);
            var audit = new RecordingAuditService();
            var uow = new FakeUnitOfWork();

            var scheduleService = new ScheduleService(
                schedules,
                medications,
                uow,
                audit,
                new CreateScheduleRequestValidator(),
                new UpdateScheduleRequestValidator());

            var adherence = new AdherenceService(schedules, logs, users, time);
            var logService = new MedicationLogService(
                medications,
                schedules,
                logs,
                users,
                uow,
                audit,
                time,
                new CreateMedicationLogRequestValidator());

            var nfc = new NfcService(tags, medications, users, uow, audit, time, adherence);

            return new Fixture
            {
                Nfc = nfc,
                LogService = logService,
                Adherence = adherence,
                Users = users,
                Tags = tags,
                Audit = audit,
                ScheduleService = scheduleService,
                Medications = medications
            };
        }

        private ScheduleService ScheduleService { get; init; } = null!;
        private InMemoryMedicationRepository Medications { get; init; } = null!;

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
                Form = "Tablet",
                IsArchived = false
            };
            Medications.Items.Add(medication);
            return medication;
        }

        public Task<ScheduleResponseDto> CreateSchedule(Guid userId, Guid medicationId, TimeOnly time) =>
            ScheduleService.CreateScheduleAsync(
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

    private sealed class MutableTimeProvider : ITimeProvider
    {
        public MutableTimeProvider(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; set; }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class RecordingAuditService : IAuditService
    {
        public List<string> Actions { get; } = [];

        public Task LogAsync(
            string action,
            string entityType,
            Guid? userId = null,
            Guid? entityId = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default,
            string? metadata = null)
        {
            Actions.Add(action);
            return Task.CompletedTask;
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

    private sealed class InMemoryNfcTagRepository : INfcTagRepository
    {
        private readonly InMemoryMedicationRepository _medications;

        public InMemoryNfcTagRepository(InMemoryMedicationRepository medications) => _medications = medications;

        public List<NfcTag> Items { get; } = [];

        public Task<NfcTag?> GetByTagIdentifierAsync(string tagIdentifier, CancellationToken cancellationToken = default)
        {
            var normalized = tagIdentifier.Trim().ToUpperInvariant();
            var tag = Items.FirstOrDefault(t => t.TagIdentifier == normalized);
            if (tag is not null)
            {
                tag.Medication = _medications.Items.First(m => m.Id == tag.MedicationId);
            }

            return Task.FromResult(tag);
        }

        public Task<NfcTag?> GetAssignedByMedicationForUserAsync(
            Guid userId,
            Guid medicationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(t =>
                t.UserId == userId && t.MedicationId == medicationId && t.IsAssigned));

        public Task<List<NfcTag>> GetAssignedForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var list = Items.Where(t => t.UserId == userId && t.IsAssigned).ToList();
            foreach (var tag in list)
            {
                tag.Medication = _medications.Items.First(m => m.Id == tag.MedicationId);
            }

            return Task.FromResult(list);
        }

        public Task AddAsync(NfcTag tag, CancellationToken cancellationToken = default)
        {
            if (tag.Id == Guid.Empty)
            {
                tag.Id = Guid.NewGuid();
            }

            tag.TagIdentifier = tag.TagIdentifier.Trim().ToUpperInvariant();
            Items.Add(tag);
            return Task.CompletedTask;
        }

        public void Update(NfcTag tag)
        {
            tag.TagIdentifier = tag.TagIdentifier.Trim().ToUpperInvariant();
        }
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

            schedule.Medication = _medications.Items.First(m => m.Id == schedule.MedicationId);
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
            var (start, end) = TimeZoneHelper.LocalDayUtcRange(localDate, timeZone);
            return Task.FromResult(Items
                .Where(l => l.UserId == userId && l.ScheduledDoseTime >= start && l.ScheduledDoseTime < end)
                .ToList());
        }

        public Task<List<MedicationLog>> GetForUserBetweenDatesAsync(
            Guid userId,
            DateOnly fromLocalDate,
            DateOnly toLocalDateInclusive,
            TimeZoneInfo timeZone,
            CancellationToken cancellationToken = default)
        {
            var (start, _) = TimeZoneHelper.LocalDayUtcRange(fromLocalDate, timeZone);
            var (_, end) = TimeZoneHelper.LocalDayUtcRange(toLocalDateInclusive, timeZone);
            return Task.FromResult(Items
                .Where(l => l.UserId == userId && l.ScheduledDoseTime >= start && l.ScheduledDoseTime < end)
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
            var list = Items.Where(l => l.UserId == userId).OrderByDescending(l => l.LoggedAt).ToList();
            foreach (var log in list)
            {
                log.Medication = _medications.Items.First(m => m.Id == log.MedicationId);
            }

            return Task.FromResult((list.Skip((page - 1) * pageSize).Take(pageSize).ToList(), list.Count));
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
