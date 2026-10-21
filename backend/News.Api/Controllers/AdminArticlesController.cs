using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using News.Application;
using News.Domain;

namespace News.Api.Controllers;
[ApiController]
[Authorize(Roles = Roles.Staff)]
[Route("api/admin/articles")]
public sealed class AdminArticlesController(ArticleManagementService service) : ControllerBase
{
    private StaffActor Actor => new(Guid.Parse(User.FindFirstValue("sub")!), User.Identity!.Name!, User.FindFirstValue("role")!);

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminArticleDto>>> List([FromQuery] AdminArticleQuery query, CancellationToken ct) =>
        Ok(await service.ListAsync(query, Actor, ct));

    [HttpGet("{id:int:min(1)}")]
    public async Task<ActionResult<AdminArticleDto>> Detail(int id, CancellationToken ct) => Ok(await service.GetAsync(id, Actor, ct));

    [HttpPost]
    public async Task<ActionResult<AdminArticleDto>> Create(SaveArticleRequest request, CancellationToken ct)
    {
        var result = await service.SaveAsync(null, request, Actor, ct);
        return CreatedAtAction(nameof(Detail), new { id = result.Article.Id }, result);
    }

    [HttpPut("{id:int:min(1)}")]
    public async Task<ActionResult<AdminArticleDto>> Update(int id, SaveArticleRequest request, CancellationToken ct) =>
        Ok(await service.SaveAsync(id, request, Actor, ct));

    [HttpPost("{id:int:min(1)}/{operation}")]
    public async Task<ActionResult<AdminArticleDto>> Transition(int id, string operation, TransitionRequest request, CancellationToken ct) =>
        Ok(await service.TransitionAsync(id, operation, request, Actor, ct));

    [HttpDelete("{id:int:min(1)}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] Guid version, CancellationToken ct)
    {
        await service.DeleteAsync(id, version, Actor, ct);
        return NoContent();
    }
}
