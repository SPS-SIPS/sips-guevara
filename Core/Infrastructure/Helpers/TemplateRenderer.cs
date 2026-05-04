namespace SPSS.Infrastructure.Helpers;

public static class TemplateRenderer
{
    // Default template content used when the template file does not exist yet.
    private const string DefaultNsiTemplate = "<!DOCTYPE html>\n<html lang=\"en\">\n  <head>\n    <meta charset=\"utf-8\" />\n    <meta http-equiv=\"x-ua-compatible\" content=\"ie=edge\" />\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />\n    <title>NSI Reconciliation Report</title>\n    <style>\n      body { font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, Helvetica, Arial, sans-serif; color: #222; }\n      .container { max-width: 640px; margin: 0 auto; padding: 24px; }\n      .muted { color: #666; font-size: 12px; }\n      .btn { display: inline-block; padding: 10px 16px; background: #1677ff; color: #fff !important; text-decoration: none; border-radius: 6px; }\n      .card { border: 1px solid #eee; border-radius: 8px; padding: 16px; margin-top: 12px; }\n      .row { margin: 6px 0; }\n      .label { color: #555; width: 160px; display: inline-block; }\n    </style>\n  </head>\n  <body>\n    <div class=\"container\">\n      <h2>NSI Reconciliation Report</h2>\n      <p>Dear {{ParticipantName}},</p>\n      <p>\n        Please find attached the reconciliation report for your institution ({{BIC}}).\n      </p>\n      <div class=\"card\">\n        <div class=\"row\"><span class=\"label\">Message Id:</span><strong>{{MsgId}}</strong></div>\n        <div class=\"row\"><span class=\"label\">Value Date:</span><strong>{{Date}}</strong></div>\n        <div class=\"row\"><span class=\"label\">Institution (BIC):</span><strong>{{BIC}}</strong></div>\n      </div>\n      <p class=\"muted\">Attachment: {{AttachmentName}}</p>\n      <p>If you have any questions, please reply to this email.</p>\n      <p>Regards,<br/>SPS Team</p>\n    </div>\n  </body>\n</html>\n";

    public static string ResolveAndEnsureTemplate(string templatesDirectory, string templateFileName)
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = string.IsNullOrWhiteSpace(templatesDirectory)
            ? Path.Combine(baseDir, "Templates")
            : (Path.IsPathRooted(templatesDirectory) ? templatesDirectory : Path.Combine(baseDir, templatesDirectory));

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var path = Path.Combine(dir, templateFileName);
        if (!File.Exists(path))
        {
            // If the NSI template is missing, write a default one.
            if (string.Equals(templateFileName, "nsi-reconciliation.html", StringComparison.OrdinalIgnoreCase))
            {
                File.WriteAllText(path, DefaultNsiTemplate);
            }
        }
        return path;
    }

    public static async Task<string> RenderAsync(string templatesDirectory, string templateFileName, IDictionary<string, string> values, CancellationToken ct = default)
    {
        var templatePath = ResolveAndEnsureTemplate(templatesDirectory, templateFileName);
        string content;
        using (var reader = new StreamReader(templatePath))
        {
            content = await reader.ReadToEndAsync();
        }
        foreach (var kv in values)
        {
            content = content.Replace("{{" + kv.Key + "}}", kv.Value ?? string.Empty);
        }
        return content;
    }

    // Backward compatible overload if a fully-qualified path is provided.
    public static async Task<string> RenderAsync(string templatePath, IDictionary<string, string> values, CancellationToken ct = default)
    {
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template not found at path: {templatePath}");
        }
        string content;
        using (var reader = new StreamReader(templatePath))
        {
            content = await reader.ReadToEndAsync();
        }
        foreach (var kv in values)
        {
            content = content.Replace("{{" + kv.Key + "}}", kv.Value ?? string.Empty);
        }
        return content;
    }
}
