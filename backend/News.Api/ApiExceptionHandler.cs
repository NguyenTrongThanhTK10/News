using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using News.Domain;

namespace News.Api;

public sealed class ApiExceptionHandler(IProblemDetailsService problems, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception error, CancellationToken ct)
    {
        var (status, title) = error switch
        {
            BusinessException business => (business.StatusCode, business.Message),
            DbUpdateConcurrencyException => (409, "Dữ liệu đã thay đổi. Hãy tải lại và thử lại."),
            _ => (500, "Có lỗi khi xử lý yêu cầu. Vui lòng thử lại sau."),
        };
        if (status == 500) logger.LogError(error, "Request failed: {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Extensions = { ["traceId"] = context.TraceIdentifier } },
        });
    }
}
