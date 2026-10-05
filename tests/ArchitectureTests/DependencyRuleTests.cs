using ArchitectureTests.Support;

namespace ArchitectureTests;

public sealed class DependencyRuleTests
{
    [Fact]
    public void Domain_references_no_project()
    {
        Assert.Empty(RepositoryLayout.ProjectReferencesOf(RepositoryLayout.DomainProject));
    }

    [Fact]
    public void Service_references_only_Domain()
    {
        Assert.Equal(new[] { RepositoryLayout.DomainProject }, RepositoryLayout.ProjectReferencesOf(RepositoryLayout.ServiceProject));
    }

    [Fact]
    public void Data_Postgres_references_only_Domain()
    {
        Assert.Equal(new[] { RepositoryLayout.DomainProject }, RepositoryLayout.ProjectReferencesOf(RepositoryLayout.DataProject));
    }

    [Fact]
    public void Util_references_Service_Data_Postgres_and_Domain()
    {
        var expected = new[] { RepositoryLayout.DomainProject, RepositoryLayout.ServiceProject, RepositoryLayout.DataProject };

        Assert.Equivalent(expected, RepositoryLayout.ProjectReferencesOf(RepositoryLayout.UtilProject));
    }

    [Fact]
    public void Facade_references_Util_Domain_and_Service_but_never_the_data_layer()
    {
        var references = RepositoryLayout.ProjectReferencesOf(RepositoryLayout.FacadeProject);

        Assert.Equivalent(new[] { RepositoryLayout.UtilProject, RepositoryLayout.DomainProject, RepositoryLayout.ServiceProject }, references);
        Assert.DoesNotContain(RepositoryLayout.DataProject, references);
    }

    [Theory]
    [InlineData(RepositoryLayout.DomainProject)]
    [InlineData(RepositoryLayout.ServiceProject)]
    [InlineData(RepositoryLayout.DataProject)]
    [InlineData(RepositoryLayout.UtilProject)]
    public void No_project_references_Facade(string project)
    {
        Assert.DoesNotContain(RepositoryLayout.FacadeProject, RepositoryLayout.ProjectReferencesOf(project));
        Assert.DoesNotContain(RepositoryLayout.FacadeProject, RepositoryLayout.ReferencedAssemblyNamesOf(project));
    }

    [Fact]
    public void Service_and_Data_Postgres_do_not_reference_each_other_in_compiled_code()
    {
        Assert.DoesNotContain(RepositoryLayout.DataProject, RepositoryLayout.ReferencedAssemblyNamesOf(RepositoryLayout.ServiceProject));
        Assert.DoesNotContain(RepositoryLayout.ServiceProject, RepositoryLayout.ReferencedAssemblyNamesOf(RepositoryLayout.DataProject));
    }

    [Fact]
    public void Service_does_not_use_data_access_or_web_libraries()
    {
        var packages = RepositoryLayout.PackageReferencesOf(RepositoryLayout.ServiceProject);

        Assert.DoesNotContain(packages, package => package.StartsWith("Npgsql", StringComparison.Ordinal) || package == "Dapper" || package.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(RepositoryLayout.ReferencedAssemblyNamesOf(RepositoryLayout.ServiceProject), name => name.StartsWith("Npgsql", StringComparison.Ordinal) || name == "Dapper" || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }
}
