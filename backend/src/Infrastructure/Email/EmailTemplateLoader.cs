namespace backend.Infrastructure.Email;

public class EmailTemplateLoader
{
    public static string LoadTemplate(string templateName, Dictionary<string, string> values)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Email", "Templates", templateName);
        var template = File.ReadAllText(path);
        foreach (var (key, value) in values)
            template = template.Replace($"{{{{{key}}}}}", value);
        return template;
    }

    public static string LoadTemplate(string templateName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Email", "Templates", templateName);
        var template = File.ReadAllText(path);
        return template;
    }
}