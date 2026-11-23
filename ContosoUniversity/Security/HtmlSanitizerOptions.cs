using System.Collections.Generic;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Allowed HTML tags/attributes policy. Stored centrally in AWS Secrets Manager (JSON)
    /// so it can be managed across multi-tenant environments without redeploying.
    /// </summary>
    public class HtmlSanitizerPolicy
    {
        public List<string> AllowedTags { get; set; } = new List<string> { "b", "i", "em", "strong", "p", "br", "ul", "ol", "li", "a" };
        public List<string> AllowedAttributes { get; set; } = new List<string> { "href", "title" };
        public List<string> AllowedSchemes { get; set; } = new List<string> { "http", "https", "mailto" };
    }
}
