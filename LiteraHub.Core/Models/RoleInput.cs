using LiteraHub.Core.Security;

namespace LiteraHub.Core.Models;

public sealed record RoleInput(string Name, string? Description, IReadOnlyCollection<RolePermission> Permissions);
