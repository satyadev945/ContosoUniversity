using Ganss.Xss;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Ganss.Xss HtmlSanitizer-backed implementation configured from an externally managed policy.
    /// </summary>
    public class HtmlSanitizerService : IHtmlSanitizerService
    {
        private readonly HtmlSanitizer _sanitizer;

        public HtmlSanitizerService(HtmlSanitizerPolicy policy)
        {
            _sanitizer = new HtmlSanitizer();
            _sanitizer.AllowedTags.Clear();
            _sanitizer.AllowedAttributes.Clear();
            _sanitizer.AllowedSchemes.Clear();

            foreach (var tag in policy.AllowedTags) _sanitizer.AllowedTags.Add(tag);
            foreach (var attr in policy.AllowedAttributes) _sanitizer.AllowedAttributes.Add(attr);
            foreach (var scheme in policy.AllowedSchemes) _sanitizer.AllowedSchemes.Add(scheme);
        }

        public string Sanitize(string html)
        {
            return string.IsNullOrEmpty(html) ? string.Empty : _sanitizer.Sanitize(html);
        }
    }
}
