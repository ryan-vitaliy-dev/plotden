namespace Application.Common.Interfaces
{
    public interface IEmailTemplateLoader
    {
        string LoadTemplate(string templateName, Dictionary<string, string>? variables = null);
    }
}