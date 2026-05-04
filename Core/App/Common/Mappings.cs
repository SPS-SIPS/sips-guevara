namespace SPSS.App.Common;

public sealed class Mappings : Profile
{
    public Mappings()
    {
        ApplyMapFromFromAssembly(typeof(RoleDto).Assembly);
        ApplyMapFromFromAssembly(Assembly.GetExecutingAssembly());
    }
    private void ApplyMapFromFromAssembly(Assembly assembly)
    {
        var types = assembly.GetExportedTypes()
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMap<>)))
            .ToList();

        foreach (var type in types)
        {
            var instance = Activator.CreateInstance(type);
            var methodInfo = type.GetMethod("Mapping");
            methodInfo?.Invoke(instance, new object[] { this });
        }
    }
}