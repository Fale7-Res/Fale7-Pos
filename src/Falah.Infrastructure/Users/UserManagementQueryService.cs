namespace Falah.Infrastructure.Users;

using Falah.Application.Users;
using Falah.Domain.Authorization;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Read-only projections for user management. Unlike GET /api/users (active users only, the
/// lock/login screen's own picker list), these include inactive accounts too - an administrator
/// reviewing or reactivating a deactivated user needs to see them.</summary>
public sealed class UserManagementQueryService
{
    private readonly FalahDbContext _db;
    public UserManagementQueryService(FalahDbContext db) => _db = db;

    public async Task<List<UserDetailsDto>> ListAllAsync(CancellationToken ct = default) =>
        (await _db.Users.AsNoTracking().ToListAsync(ct)).Select(ToDto).ToList();

    public async Task<UserDetailsDto?> GetAsync(string id, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        return user is null ? null : ToDto(user);
    }

    public IReadOnlyList<PermissionDefinition> GetPermissionCatalog() => PermissionCatalog.All;

    private static UserDetailsDto ToDto(Falah.Infrastructure.Persistence.Entities.UserEntity u) => new(
        u.Id, u.Name, u.Role, u.Active,
        u.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList());
}
