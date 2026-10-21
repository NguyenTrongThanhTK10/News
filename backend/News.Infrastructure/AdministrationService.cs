using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using News.Application;
using News.Domain;

namespace News.Infrastructure;

public sealed class AdministrationService(NewsDbContext db, IPasswordHasher<User> hasher)
{
    public async Task<PagedResult<ManagedUserDto>> ListUsersAsync(UserManagementQuery query, CancellationToken ct)
    {
        var users = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            users = users.Where(u => u.Name.ToLower().Contains(search) || u.Email.Contains(search));
        }
        var total = await users.CountAsync(ct);
        var page = users.OrderBy(u => u.Name).ThenBy(u => u.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize);
        var items = await (from user in page
            join role in db.RoleDefinitions on (user.RoleDefinitionId ?? user.Role) equals role.Id into roles
            from role in roles.DefaultIfEmpty()
            orderby user.Name, user.Id
            select new ManagedUserDto(user.Id, user.Name, user.Email, user.Role,
                user.RoleDefinitionId ?? user.Role, role == null ? user.Role : role.Name, user.Version)).ToArrayAsync(ct);
        return new(items, total, query.Page, query.PageSize);
    }

    public async Task<ManagedUserDto> CreateUserAsync(CreateUserRequest request, CancellationToken ct)
    {
        var role = await FindRoleAsync(request.RoleId, ct);
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct)) throw new BusinessException(409, "Email này đã được sử dụng.");
        var user = new User { Name = request.Name.Trim(), Email = email, Role = role.BaseRole, RoleDefinitionId = role.Id };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await SaveUniqueAsync("Email này đã được sử dụng.", ct);
        return new(user.Id, user.Name, user.Email, user.Role, role.Id, role.Name, user.Version);
    }

    public async Task<ManagedUserDto> AssignRoleAsync(Guid id, Guid actorId, AssignRoleRequest request, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new BusinessException(404, "Không tìm thấy người dùng.");
        var role = await FindRoleAsync(request.RoleId, ct);
        if (user.Version != request.Version) throw new BusinessException(409, "Người dùng đã thay đổi. Hãy tải lại danh sách.");
        if (id == actorId && role.BaseRole != Roles.Admin)
            throw new BusinessException(400, "Bạn không thể tự gỡ quyền quản trị của mình.");
        if ((user.RoleDefinitionId ?? user.Role) == role.Id)
            return new(user.Id, user.Name, user.Email, user.Role, role.Id, role.Name, user.Version);
        user.Role = role.BaseRole;
        user.RoleDefinitionId = role.Id;
        user.Version = Guid.NewGuid();
        // Thu hồi cả refresh token khi đổi vai trò để quyền cũ không tiếp tục được sử dụng.
        db.Sessions.RemoveRange(await db.Sessions.Where(s => s.UserId == id).ToArrayAsync(ct));
        await db.SaveChangesAsync(ct);
        return new(user.Id, user.Name, user.Email, user.Role, role.Id, role.Name, user.Version);
    }

    public async Task<IReadOnlyList<RoleDefinitionDto>> ListRolesAsync(CancellationToken ct) =>
        await db.RoleDefinitions.AsNoTracking().OrderBy(r => r.Name)
            .Select(r => new RoleDefinitionDto(r.Id, r.Name, r.BaseRole,
                db.Users.Count(u => (u.RoleDefinitionId ?? u.Role) == r.Id))).ToArrayAsync(ct);

    public async Task<RoleDefinitionDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (await db.RoleDefinitions.AnyAsync(r => r.Name.ToLower() == name.ToLower(), ct))
            throw new BusinessException(409, "Tên vai trò đã tồn tại.");
        var role = new RoleDefinition { Id = Guid.NewGuid().ToString("N"), Name = name, BaseRole = request.BaseRole };
        db.RoleDefinitions.Add(role);
        await SaveUniqueAsync("Tên vai trò đã tồn tại.", ct);
        return new(role.Id, role.Name, role.BaseRole, 0);
    }

    public async Task<IReadOnlyList<CategoryDto>> ListCategoriesAsync(CancellationToken ct) =>
        await db.Categories.AsNoTracking().OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Slug, c.Name, db.Articles.Count(a => a.CategorySlug == c.Slug && !a.IsDeleted))).ToArrayAsync(ct);

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (await db.Categories.AnyAsync(c => c.Slug == request.Slug || c.Name.ToLower() == name.ToLower(), ct))
            throw new BusinessException(409, "Tên hoặc slug danh mục đã tồn tại.");
        db.Categories.Add(new Category { Slug = request.Slug, Name = name });
        await SaveUniqueAsync("Slug danh mục đã tồn tại.", ct);
        return new(request.Slug, name, 0);
    }

    private async Task<RoleDefinition> FindRoleAsync(string id, CancellationToken ct) =>
        await db.RoleDefinitions.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, ct)
        ?? throw new BusinessException(400, "Vai trò không tồn tại. Hãy chọn lại vai trò.");

    private async Task SaveUniqueAsync(string message, CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException error) when (
            error.InnerException is Microsoft.Data.Sqlite.SqliteException { SqliteErrorCode: 19, SqliteExtendedErrorCode: 1555 or 2067 }
            or Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        { throw new BusinessException(409, message); }
    }
}
