namespace Falah.Infrastructure.Employees;

using Falah.Application.Employees;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Employees;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from addEmployee in lib/store.tsx - gated on employees_manage, idempotent via
/// the same deterministic-id-then-replay pattern as every other create command in this codebase.
/// Never accepts UserId (see Employee's own doc comment).</summary>
public sealed class CreateEmployeeHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public CreateEmployeeHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<EmployeeDto> HandleAsync(CreateEmployeeRequest request, CancellationToken ct = default)
    {
        var id = $"emp-{request.RequestId}";
        var existing = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        if (existing is not null) return EmployeeQueryService.ToDto(existing);

        EmployeeRules.AssertEmployeeInput(request.Name, request.EmployeeNumber, request.JobTitle, request.Phone);
        if (!Enum.TryParse<CompensationType>(request.CompensationType, ignoreCase: true, out _))
            throw new DomainException("نوع الأجر غير صالح.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "employees_manage");

        var entity = new EmployeeEntity
        {
            Id = id, Name = request.Name.Trim(), EmployeeNumber = request.EmployeeNumber.Trim(), JobTitle = request.JobTitle.Trim(),
            Phone = request.Phone.Trim(), HireDate = request.HireDate, CompensationType = request.CompensationType,
            DailyWagePiastres = request.DailyWagePiastres, ShiftValuePiastres = request.ShiftValuePiastres,
            MonthlySalaryPiastres = request.MonthlySalaryPiastres, UserId = null, CanDeliver = request.CanDeliver,
            Role = request.Role, IsActive = true, Notes = request.Notes, Address = request.Address,
            NationalId = request.NationalId, Department = request.Department, EmploymentArrangement = request.EmploymentArrangement,
            FingerprintId = request.FingerprintId, LifecycleStatus = request.LifecycleStatus ?? nameof(EmployeeLifecycleStatus.Active),
            OvertimeRatePiastres = request.OvertimeRatePiastres, CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        try
        {
            _db.Employees.Add(entity);
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
            if (replay is null) throw;
            return EmployeeQueryService.ToDto(replay);
        }
        return EmployeeQueryService.ToDto(entity);
    }
}

/// <summary>Ported from updateEmployee - a full profile replace. UserId is never accepted here
/// (preserved as-is), matching legacy's own `{...employee, pin: e.pin, userId: e.userId}`.</summary>
public sealed class UpdateEmployeeHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public UpdateEmployeeHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<EmployeeDto> HandleAsync(UpdateEmployeeRequest request, CancellationToken ct = default)
    {
        EmployeeRules.AssertEmployeeInput(request.Name, request.EmployeeNumber, request.JobTitle, request.Phone);
        if (!Enum.TryParse<CompensationType>(request.CompensationType, ignoreCase: true, out _))
            throw new DomainException("نوع الأجر غير صالح.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "employees_manage");

        var entity = await _db.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct)
            ?? throw new InvalidOperationException("الموظف غير موجود.");

        entity.Name = request.Name.Trim();
        entity.EmployeeNumber = request.EmployeeNumber.Trim();
        entity.JobTitle = request.JobTitle.Trim();
        entity.Phone = request.Phone.Trim();
        entity.HireDate = request.HireDate;
        entity.CompensationType = request.CompensationType;
        entity.DailyWagePiastres = request.DailyWagePiastres;
        entity.ShiftValuePiastres = request.ShiftValuePiastres;
        entity.MonthlySalaryPiastres = request.MonthlySalaryPiastres;
        entity.CanDeliver = request.CanDeliver;
        entity.Role = request.Role;
        entity.IsActive = request.IsActive;
        entity.Notes = request.Notes;
        entity.Address = request.Address;
        entity.NationalId = request.NationalId;
        entity.Department = request.Department;
        entity.EmploymentArrangement = request.EmploymentArrangement;
        entity.FingerprintId = request.FingerprintId;
        entity.LifecycleStatus = request.LifecycleStatus;
        entity.OvertimeRatePiastres = request.OvertimeRatePiastres;

        await _db.SaveChangesAsync(ct);
        return EmployeeQueryService.ToDto(entity);
    }
}
