using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using News.Application;
using News.Domain;
using News.Infrastructure;

namespace News.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin")]
public sealed class AdministrationController(AdministrationService service) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<ManagedUserDto>>> Users([FromQuery] UserManagementQuery query, CancellationToken ct) =>
        Ok(await service.ListUsersAsync(query, ct));

    [HttpPost("users")]
    public async Task<ActionResult<ManagedUserDto>> CreateUser(CreateUserRequest request, CancellationToken ct)
    {
        var user = await service.CreateUserAsync(request, ct);
        return Created($"/api/admin/users?search={Uri.EscapeDataString(user.Email)}", user);
    }

    [HttpPut("users/{id:guid}/role")]
    public async Task<ActionResult<ManagedUserDto>> AssignRole(Guid id, AssignRoleRequest request, CancellationToken ct) =>
        Ok(await service.AssignRoleAsync(id, Guid.Parse(User.FindFirstValue("sub")!), request, ct));

    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<RoleDefinitionDto>>> RolesList(CancellationToken ct) =>
        Ok(await service.ListRolesAsync(ct));

    [HttpPost("roles")]
    public async Task<ActionResult<RoleDefinitionDto>> CreateRole(CreateRoleRequest request, CancellationToken ct)
    {
        var role = await service.CreateRoleAsync(request, ct);
        return Created("/api/admin/roles", role);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> Categories(CancellationToken ct) =>
        Ok(await service.ListCategoriesAsync(ct));

    [HttpPost("categories")]
    public async Task<ActionResult<CategoryDto>> CreateCategory(CreateCategoryRequest request, CancellationToken ct)
    {
        var category = await service.CreateCategoryAsync(request, ct);
        return Created("/api/categories", category);
    }
}
