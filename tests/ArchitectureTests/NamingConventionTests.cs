using ArchitectureTests.Support;
using MediatR;

namespace ArchitectureTests;

public sealed class NamingConventionTests
{
    private static readonly string[] ReservedNames = ["Domain", "Service", "Data", "Util", "Facade"];

    [Theory]
    [InlineData(RepositoryLayout.DomainProject)]
    [InlineData(RepositoryLayout.ServiceProject)]
    [InlineData(RepositoryLayout.DataProject)]
    [InlineData(RepositoryLayout.UtilProject)]
    [InlineData(RepositoryLayout.FacadeProject)]
    public void Each_project_uses_its_folder_name_as_root_namespace(string project)
    {
        var namespaces = RepositoryLayout.AssemblyOf(project)
            .GetTypes()
            .Where(type => type.Namespace is not null && !type.Namespace.StartsWith("Microsoft", StringComparison.Ordinal) && !type.Namespace.StartsWith("System", StringComparison.Ordinal))
            .Select(type => type.Namespace!)
            .Distinct();

        Assert.All(namespaces, ns => Assert.True(ns == project || ns.StartsWith(project + ".", StringComparison.Ordinal), $"'{ns}' is outside the root namespace '{project}'."));
    }

    [Fact]
    public void Test_namespaces_never_shadow_the_source_namespaces()
    {
        var namespaces = typeof(NamingConventionTests).Assembly.GetTypes()
            .Where(type => type.Namespace is not null)
            .SelectMany(type => type.Namespace!.Split('.'))
            .Distinct();

        Assert.DoesNotContain(namespaces, segment => ReservedNames.Contains(segment));
    }

    [Fact]
    public void Test_folders_never_use_reserved_names()
    {
        var folders = Directory.GetDirectories(Path.Combine(RepositoryLayout.RootPath, "tests"), "*", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin") && !path.Contains($"{Path.DirectorySeparatorChar}obj"))
            .Select(Path.GetFileName);

        Assert.DoesNotContain(folders, folder => ReservedNames.Contains(folder!));
    }

    [Fact]
    public void Facade_contracts_are_not_commands_or_queries()
    {
        var contractNames = RepositoryLayout.AssemblyOf(RepositoryLayout.FacadeProject).GetTypes()
            .Where(type => type.Namespace == "Facade.Contracts")
            .Select(type => type.Name);

        Assert.DoesNotContain(contractNames, name => name.EndsWith("Command", StringComparison.Ordinal) || name.EndsWith("Query", StringComparison.Ordinal));
    }

    [Fact]
    public void Service_requests_are_commands_or_queries_and_never_request_response_contracts()
    {
        var serviceTypes = RepositoryLayout.AssemblyOf(RepositoryLayout.ServiceProject).GetTypes();
        var requests = serviceTypes.Where(type => type.GetInterfaces().Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequest<>)));

        Assert.All(requests, type => Assert.True(type.Name.EndsWith("Command", StringComparison.Ordinal) || type.Name.EndsWith("Query", StringComparison.Ordinal), $"{type.Name} must end with Command or Query."));
        Assert.DoesNotContain(serviceTypes, type => type.Name.EndsWith("Request", StringComparison.Ordinal) || type.Name.EndsWith("Response", StringComparison.Ordinal));
    }

    [Fact]
    public void Every_handler_lives_next_to_its_use_case_in_Service()
    {
        var handlers = RepositoryLayout.AssemblyOf(RepositoryLayout.ServiceProject).GetTypes()
            .Where(type => type.GetInterfaces().Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)));

        Assert.All(handlers, type =>
        {
            Assert.True(type.Name.EndsWith("Handler", StringComparison.Ordinal), $"{type.Name} must end with Handler.");
            Assert.True(type.Namespace!.StartsWith("Service.Commands.", StringComparison.Ordinal) || type.Namespace.StartsWith("Service.Queries.", StringComparison.Ordinal), $"{type.FullName} must live under Commands or Queries.");
            Assert.True(type.IsSealed, $"{type.Name} must be sealed.");
        });
    }

    [Fact]
    public void Handlers_and_repositories_are_not_referenced_by_Facade_types()
    {
        var facadeTypes = RepositoryLayout.AssemblyOf(RepositoryLayout.FacadeProject).GetTypes().Where(type => type.Namespace?.StartsWith("Facade.Endpoints", StringComparison.Ordinal) == true);
        var repositoryInterfaces = RepositoryLayout.AssemblyOf(RepositoryLayout.DomainProject).GetTypes().Where(type => type.Namespace == "Domain.Repositories").ToHashSet();

        var offenders = facadeTypes
            .SelectMany(type => type.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly))
            .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType))
            .Where(repositoryInterfaces.Contains);

        Assert.Empty(offenders);
    }
}
