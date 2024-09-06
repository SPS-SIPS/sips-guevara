namespace SPSS.Domain.Interfaces;

public interface IRolesCache
{
    List<ExtendedRoleDto> _roles { get; }
    Task<ExtendedRoleDto?> GetRole(Guid Id);
    Task<List<ExtendedRoleDto>> GetRoles();
    Task<List<ExtendedRoleDto>> GetRoles(Guid[] Ids);
    Task<string?> GetParentRole(string claimType);

    void Invalidate();

}
