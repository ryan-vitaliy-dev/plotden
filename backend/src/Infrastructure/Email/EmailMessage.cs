namespace backend.Infrastructure.Email
{
    public class EmailMessage(string To, string Subject, string Body)
    {
        public string To { get; set; } = To;
        public string Subject { get; set; } = Subject;
        public string Body { get; set; } = Body;
    }
}