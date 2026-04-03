using Application.Common.Interfaces;

namespace Infrastructure.Email;

public class EmailTemplateLoader : IEmailTemplateLoader
{
    public string LoadTemplate(string templateName, Dictionary<string, string>? values)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Email", "Templates", templateName);
        var template = File.ReadAllText(path);
        if(values != null && values.Count > 0)
        {
            foreach (var (key, value) in values)
            {
                template = template.Replace($"{{{{{key}}}}}", value);
            }
        }
        return template;
    }
}