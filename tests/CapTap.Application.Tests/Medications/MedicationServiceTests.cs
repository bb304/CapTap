using CapTap.Application.DTOs.Medication;
using CapTap.Application.Exceptions;
using CapTap.Application.Interfaces;
using CapTap.Application.Services;
using CapTap.Application.Validators;
using CapTap.Domain.Exceptions;
using FluentAssertions;
using MedicationEntity = CapTap.Domain.Entities.Medication;
using ValidationException = CapTap.Application.Exceptions.ValidationException;

namespace CapTap.Application.Tests.Medications;

public sealed class MedicationServiceTests
{
    [Fact]
    public async Task CreateMedication_SavesForAuthenticatedUser()
    {
        var fixture = MedicationTestFixture.Create();
        var userId = Guid.NewGuid();

        var result = await fixture.Sut.CreateMedicationAsync(
            userId,
            new CreateMedicationRequest
            {
                Name = "Metformin",
                GenericName = "Metformin Hydrochloride",
                BrandName = "Glucophage",
                DosageAmount = 500,
                DosageUnit = "mg",
                Form = "Tablet"
            });

        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Metformin");
        fixture.Medications.Items.Should().ContainSingle(m => m.UserId == userId && m.Name == "Metformin");
        fixture.Audit.Actions.Should().Contain("MEDICATION_CREATED");
    }

    [Fact]
    public async Task GetUserMedications_ReturnsOnlyOwnActiveMedications()
    {
        var fixture = MedicationTestFixture.Create();
        var userId = Guid.NewGuid();

        await fixture.Sut.CreateMedicationAsync(
            userId,
            new CreateMedicationRequest { Name = "Metformin", DosageAmount = 500, DosageUnit = "mg" });
        await fixture.Sut.CreateMedicationAsync(
            Guid.NewGuid(),
            new CreateMedicationRequest { Name = "Lisinopril", DosageAmount = 10, DosageUnit = "mg" });

        var list = await fixture.Sut.GetUserMedicationsAsync(userId);

        list.Should().ContainSingle();
        list[0].Name.Should().Be("Metformin");
    }

    [Fact]
    public async Task GetMedication_CannotAccessAnotherUsersMedication()
    {
        var fixture = MedicationTestFixture.Create();
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var created = await fixture.Sut.CreateMedicationAsync(
            ownerId,
            new CreateMedicationRequest { Name = "Metformin", DosageAmount = 500, DosageUnit = "mg" });

        var act = () => fixture.Sut.GetMedicationAsync(otherUserId, created.Id);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Medication was not found.");
    }

    [Fact]
    public async Task ArchiveMedication_SetsIsArchivedTrue()
    {
        var fixture = MedicationTestFixture.Create();
        var userId = Guid.NewGuid();

        var created = await fixture.Sut.CreateMedicationAsync(
            userId,
            new CreateMedicationRequest { Name = "Metformin", DosageAmount = 500, DosageUnit = "mg" });

        await fixture.Sut.ArchiveMedicationAsync(userId, created.Id);

        fixture.Medications.Items.Single(m => m.Id == created.Id).IsArchived.Should().BeTrue();
        fixture.Audit.Actions.Should().Contain("MEDICATION_ARCHIVED");
        (await fixture.Sut.GetUserMedicationsAsync(userId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Search_WhenFdaFails_ReturnsGracefulError()
    {
        var fixture = MedicationTestFixture.Create(fdaShouldFail: true);
        var userId = Guid.NewGuid();

        var act = () => fixture.Sut.SearchMedicationsAsync(userId, "metformin");

        await act.Should().ThrowAsync<InvalidRequestException>()
            .WithMessage("*temporarily unavailable*");
    }

    [Fact]
    public async Task CreateMedication_InvalidDosage_IsRejected()
    {
        var fixture = MedicationTestFixture.Create();

        var act = () => fixture.Sut.CreateMedicationAsync(
            Guid.NewGuid(),
            new CreateMedicationRequest
            {
                Name = "Metformin",
                DosageAmount = -5,
                DosageUnit = "mg"
            });

        await act.Should().ThrowAsync<ValidationException>();
        fixture.Medications.Items.Should().BeEmpty();
    }

    private sealed class MedicationTestFixture
    {
        public required MedicationService Sut { get; init; }
        public required InMemoryMedicationRepository Medications { get; init; }
        public required FakeAuditService Audit { get; init; }

        public static MedicationTestFixture Create(bool fdaShouldFail = false)
        {
            var medications = new InMemoryMedicationRepository();
            var audit = new FakeAuditService();
            var fda = new FakeFdaMedicationService(fdaShouldFail);

            var sut = new MedicationService(
                medications,
                new FakeUnitOfWork(),
                fda,
                audit,
                new CreateMedicationRequestValidator(),
                new UpdateMedicationRequestValidator());

            return new MedicationTestFixture
            {
                Sut = sut,
                Medications = medications,
                Audit = audit
            };
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class InMemoryMedicationRepository : IMedicationRepository
    {
        public List<MedicationEntity> Items { get; } = [];

        public Task<List<MedicationEntity>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Where(m => m.UserId == userId && !m.IsArchived).OrderBy(m => m.Name).ToList());

        public Task<MedicationEntity?> GetByIdForUserAsync(Guid userId, Guid medicationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(m => m.Id == medicationId && m.UserId == userId));

        public Task AddAsync(MedicationEntity medication, CancellationToken cancellationToken = default)
        {
            if (medication.Id == Guid.Empty)
            {
                medication.Id = Guid.NewGuid();
            }

            Items.Add(medication);
            return Task.CompletedTask;
        }

        public void Update(MedicationEntity medication)
        {
            // tracked in-memory
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

    private sealed class FakeFdaMedicationService : IFdaMedicationService
    {
        private readonly bool _shouldFail;

        public FakeFdaMedicationService(bool shouldFail) => _shouldFail = shouldFail;

        public Task<List<MedicationSearchResultDto>> SearchMedicationAsync(
            string query,
            CancellationToken cancellationToken = default)
        {
            if (_shouldFail)
            {
                throw new InvalidRequestException("Medication search is temporarily unavailable. Please try again later.");
            }

            return Task.FromResult(new List<MedicationSearchResultDto>
            {
                new() { Name = "Metformin", Brand = "Glucophage", Identifier = "NDC-1" }
            });
        }
    }

    private sealed class FakeAuditService : IAuditService
    {
        public List<string> Actions { get; } = [];

        public Task LogAsync(
            string action,
            string entityType,
            Guid? userId = null,
            Guid? entityId = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            Actions.Add(action);
            return Task.CompletedTask;
        }
    }
}
