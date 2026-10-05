using ArchitectureTests.Support;

namespace ArchitectureTests;

public sealed class DomainPurityTests
{
    private static readonly string[] ForbiddenPrefixes =
    [
        "Npgsql",
        "Dapper",
        "Oracle",
        "System.Data",
        "Microsoft.Data",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Microsoft.Extensions",
        "MediatR",
        "FluentValidation",
        "Serilog",
        "Polly",
        "OpenTelemetry",
        "System.Net.Http",
        "System.IdentityModel",
        "Microsoft.IdentityModel"
    ];

    [Fact]
    public void Domain_has_no_package_references()
    {
        Assert.Empty(RepositoryLayout.PackageReferencesOf(RepositoryLayout.DomainProject));
    }

    [Fact]
    public void Domain_depends_on_no_data_messaging_or_web_library()
    {
        var referenced = RepositoryLayout.ReferencedAssemblyNamesOf(RepositoryLayout.DomainProject);

        Assert.DoesNotContain(referenced, name => ForbiddenPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)));
    }

    [Fact]
    public void Domain_only_depends_on_the_base_class_library()
    {
        var referenced = RepositoryLayout.ReferencedAssemblyNamesOf(RepositoryLayout.DomainProject);

        Assert.All(referenced, name => Assert.True(
            name.StartsWith("System", StringComparison.Ordinal) || name is "netstandard" or "mscorlib",
            $"Domain references '{name}', which is not part of the base class library."));
    }

    [Fact]
    public void Domain_namespaces_stay_inside_the_Domain_root()
    {
        var namespaces = RepositoryLayout.AssemblyOf(RepositoryLayout.DomainProject)
            .GetTypes()
            .Where(type => type.Namespace is not null)
            .Select(type => type.Namespace!)
            .Distinct();

        Assert.All(namespaces, ns => Assert.StartsWith("Domain", ns, StringComparison.Ordinal));
    }
}
