using SPSS.Domain.Enums;
using static SPSS.Domain.Common.Enumeration<int>;

namespace SPSS.API.Controllers;
[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class EnumerationsController : BaseController
{

    [HttpGet("ServiceTypes")]
    public Response<List<Lookup<int>>> ServiceTypes()
    {
        var data = GetAll<ServiceType>()
        .Select(x => new Lookup<int>(x.Id, x.Name, x.GroupId))
        .ToList();
        return new Response<List<Lookup<int>>>(data);
    }

    [HttpGet("ServiceStatuses")]
    public Response<List<Lookup<int>>> ServiceStatuses()
    {
        var data = GetAll<ServiceStatus>()
        .Select(x => new Lookup<int>(x.Id, x.Name, x.GroupId))
        .ToList();
        return new Response<List<Lookup<int>>>(data);
    }

    [HttpGet("fileTypes")]
    public Response<List<Lookup<string>>> FileTypes()
    {
        return new Response<List<Lookup<string>>>([
            new Lookup<string>("csv", "Tabular", ""),
            new Lookup<string>("xml", "XML", ""),
        ]);
    }
}
