using System.ComponentModel.DataAnnotations;

namespace News.Application;

public sealed record RoleDefinitionDto(string Id, string Name, string BaseRole, int UserCount);
public sealed record ManagedUserDto(Guid Id, string Name, string Email, string Role, string RoleId, string RoleName, Guid Version);

public sealed class CreateUserRequest
{
    [Required, StringLength(100)] public string Name { get; init; } = "";
    [Required, EmailAddress, StringLength(254)] public string Email { get; init; } = "";
    [Required, StringLength(200, MinimumLength = 12)] public string Password { get; init; } = "";
    [Required, StringLength(80)] public string RoleId { get; init; } = "";
}

public sealed class AssignRoleRequest
{
    [Required, StringLength(80)] public string RoleId { get; init; } = "";
    [Required] public Guid? Version { get; init; }
}

public sealed class CreateRoleRequest
{
    [Required, StringLength(80)] public string Name { get; init; } = "";
    [Required, RegularExpression("^(Admin|Editor|Author|Reader)$")] public string BaseRole { get; init; } = "";
}

public sealed class CreateCategoryRequest
{
    [Required, StringLength(80)] public string Name { get; init; } = "";
    [Required, StringLength(80), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Slug chỉ gồm chữ thường a-z, số và dấu gạch nối giữa các từ.")]
    public string Slug { get; init; } = "";
}

public sealed class UserManagementQuery
{
    [StringLength(254)] public string? Search { get; init; }
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 50)] public int PageSize { get; init; } = 10;
}
