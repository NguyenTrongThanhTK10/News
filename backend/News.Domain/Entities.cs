namespace News.Domain;

public sealed class Category
{
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? GoogleSubject { get; set; }
    public string Role { get; set; } = Roles.Author;
    public string? RoleDefinitionId { get; set; }
    public int FailedLogins { get; set; }
    public DateTime? LockedUntil { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
}

// Vai trò tùy chỉnh kế thừa một mức quyền có sẵn trong quy trình biên tập.
public sealed class RoleDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string BaseRole { get; set; } = Roles.Author;
}

public sealed class LoginSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string RefreshTokenHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
}
