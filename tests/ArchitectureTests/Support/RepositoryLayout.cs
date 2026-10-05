using System.Reflection;
using System.Xml.Linq;

namespace ArchitectureTests.Support;

internal static class RepositoryLayout
{
    public const string DomainProject = "Domain";
    public const string ServiceProject = "Service";
    public const string DataProject = "Data.Postgres";
    public const string UtilProject = "Util";
    public const string FacadeProject = "Facade";

    public static readonly string[] SourceProjects = [DomainProject, ServiceProject, DataProject, UtilProject, FacadeProject];

    private static readonly Lazy<string> Root = new(FindRoot);

    public static string RootPath => Root.Value;

    public static IReadOnlySet<string> ProjectReferencesOf(string project) =>
        XDocument.Load(Path.Combine(RootPath, "src", project, $"{project}.csproj"))
            .Descendants("ProjectReference")
            .Select(reference => Path.GetFileNameWithoutExtension(reference.Attribute("Include")!.Value.Replace('\\', '/')))
            .ToHashSet();

    public static IReadOnlySet<string> PackageReferencesOf(string project) =>
        XDocument.Load(Path.Combine(RootPath, "src", project, $"{project}.csproj"))
            .Descendants("PackageReference")
            .Select(reference => reference.Attribute("Include")!.Value)
            .ToHashSet();

    public static Assembly AssemblyOf(string project) => Assembly.Load(project);

    public static IReadOnlySet<string> ReferencedAssemblyNamesOf(string project) =>
        AssemblyOf(project).GetReferencedAssemblies().Select(name => name.Name!).ToHashSet();

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "workshop-manager.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("workshop-manager.sln was not found above the test output folder.");
    }
}
