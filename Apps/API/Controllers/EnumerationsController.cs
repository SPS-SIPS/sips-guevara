namespace SPSS.API.Controllers;
[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class EnumerationsController : BaseController
{
    [HttpGet("fileTypes")]
    public Response<List<Lookup<string>>> FileTypes()
    {
        return new Response<List<Lookup<string>>>([
            new Lookup<string>("csv", "Tabular", ""),
            new Lookup<string>("xml", "XML", ""),
        ]);
    }
}
