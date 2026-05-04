namespace SPSS.API.Filters;

public sealed class ResponseActionFilter : IActionFilter
{
    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is ObjectResult objectResult)
        {
            if (context.Result is ObjectResult result)
            {
                if (result.Value is IResponse response)
                {
                    result.StatusCode = response.StatusCode;
                }
            }
        }
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
    }
}