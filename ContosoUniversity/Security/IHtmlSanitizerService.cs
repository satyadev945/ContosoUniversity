namespace ContosoUniversity.Security
{
    /// <summary>
    /// Sanitizes untrusted HTML before it is rendered. Use instead of Html.Raw()/HtmlString.
    /// </summary>
    public interface IHtmlSanitizerService
    {
        string Sanitize(string html);
    }
}
