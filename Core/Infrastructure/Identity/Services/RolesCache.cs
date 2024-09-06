using AutoMapper;
using AutoMapper.QueryableExtensions;
using SPSS.Domain.Models.DTOs;

namespace SPSS.Infrastructure.Identity.Services;
public sealed class RolesCache(IServiceProvider serviceProvider) : IRolesCache
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    public List<ExtendedRoleDto> _roles { get; private set; } = new List<ExtendedRoleDto>();
    private bool isValid = false;

    public async Task Load()
    {
        if (isValid)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();

        var _context = scope.ServiceProvider.GetRequiredService<IStorageBroker>();
        var _mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        _roles = await _context.Roles.ProjectTo<ExtendedRoleDto>(_mapper.ConfigurationProvider).ToListAsync();
        isValid = true;
    }
    public async Task<List<ExtendedRoleDto>> GetRoles()
    {
        await Load();
        return _roles;
    }
    public async Task<ExtendedRoleDto?> GetRole(Guid Id)
    {
        await Load();
        return _roles.Where(x => x.Id.Equals(Id)).FirstOrDefault();
    }

    public async Task<List<ExtendedRoleDto>> GetRoles(Guid[] Ids)
    {
        await Load();
        return _roles.Where(x => Ids.Contains(x.Id)).ToList();
    }

    public async Task<string?> GetParentRole(string claimType)
    {
        await Load();
        return _roles.Where(x => x.Claims.Any(y => y.Type.Equals(claimType))).Select(x => x.Name).FirstOrDefault();
    }

    public void Invalidate()
    {
        isValid = false;
    }
}