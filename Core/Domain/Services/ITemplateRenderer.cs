namespace SPSS.Domain.Services;

public interface ITemplateRenderer
{
    Task<string> RenderAsync(string templatesDirectory, string templateFileName, IDictionary<string, string> values, CancellationToken cancellationToken = default);
}
