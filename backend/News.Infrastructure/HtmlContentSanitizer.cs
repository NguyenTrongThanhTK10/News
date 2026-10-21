using Ganss.Xss;
using News.Application;

namespace News.Infrastructure;

public sealed class HtmlContentSanitizer : IHtmlContentSanitizer
{
    public string Sanitize(string html)
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        foreach (var tag in new[] { "p", "br", "strong", "b", "em", "i", "u", "s", "h2", "h3", "ol", "ul", "li", "blockquote", "a", "code", "pre" })
            sanitizer.AllowedTags.Add(tag);
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.Add("href");
        sanitizer.AllowedAttributes.Add("title");
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowDataAttributes = false;
        return sanitizer.Sanitize(html);
    }
}
