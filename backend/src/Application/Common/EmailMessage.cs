namespace Application.Common
{
    public class EmailMessage(string To, string Subject, string HtmlBody, string? PlainTextBody = null)
    {
        public string To { get; set; } = To;
        public string Subject { get; set; } = Subject;
        public string HtmlBody { get; set; } = HtmlBody;
        public string? PlainTextBody { get; set; } = PlainTextBody;
    }
}