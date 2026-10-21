using Microsoft.AspNetCore.Mvc;
using News.Application;

namespace News.Api.Controllers;
[ApiController]
[Route("api/articles")]
public sealed class ArticlesController(IArticleService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ArticleSummary>>> GetAll([FromQuery] ArticleQuery query, CancellationToken ct) =>
        Ok(await service.GetArticlesAsync(query, ct));

    [HttpGet("{id:int:min(1)}")]
    public async Task<ActionResult<ArticleDetail>> GetById(int id, CancellationToken ct)
    {
        var article = await service.GetArticleAsync(id, ct);
        return article is null ? Problem(statusCode: 404, title: "Không tìm thấy bài viết", detail: "Bài viết không tồn tại hoặc chưa xuất bản.") : Ok(article);
    }
}
