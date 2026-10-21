using Microsoft.AspNetCore.Mvc;
using News.Application;
namespace News.Api.Controllers;
[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(IArticleService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken ct) => Ok(await service.GetCategoriesAsync(ct));
}
