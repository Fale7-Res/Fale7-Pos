namespace Falah.Domain.Authorization;

/// <summary>
/// The user attempting a command. Ported from lib/authorization.mjs's actor shape: a flat
/// permission list (or the literal "all") plus an active flag - there is no role-based implicit
/// grant anywhere except through that list, which is exactly what makes revoking a permission on
/// the Users screen actually take effect everywhere immediately.
/// </summary>
public sealed record Actor(string Id, string Name, string Role, bool Active, IReadOnlySet<string> Permissions);
