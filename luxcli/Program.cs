using CommandLine;
using Luxoria.CLI.Models;
using System.Diagnostics;

Parser.Default.ParseArguments<BuildOptions, ListOptions>(args)
    .WithParsed<BuildOptions>(opts => RunBuildCommand(opts))
    .WithParsed<ListOptions>(opts => RunListCommand(opts))
    .WithNotParsed(errs => HandleParseError(errs));

static void RunBuildCommand(BuildOptions opts)
{
    if (opts.Verbose)
        Console.WriteLine($"Running build command in directory: {opts.Dir ?? Directory.GetCurrentDirectory()}");

    var workingDir = opts.Dir ?? Directory.GetCurrentDirectory();
    
    if (!Directory.Exists(workingDir))
    {
        Console.WriteLine($"Error: Directory '{workingDir}' does not exist.");
        Environment.Exit(1);
    }

    try
    {
        if (opts.All)
        {
            BuildAllModules(workingDir, opts);
        }
        else if (!string.IsNullOrEmpty(opts.Module))
        {
            BuildSpecificModule(workingDir, opts.Module, opts);
        }
        else
        {
            BuildCurrentProject(workingDir, opts);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Build failed: {ex.Message}");
        Environment.Exit(1);
    }
}

static void RunListCommand(ListOptions opts)
{
    if (opts.Verbose)
        Console.WriteLine($"Running list command in directory: {opts.Dir ?? Directory.GetCurrentDirectory()}");

    var workingDir = opts.Dir ?? Directory.GetCurrentDirectory();
    
    if (!Directory.Exists(workingDir))
    {
        Console.WriteLine($"Error: Directory '{workingDir}' does not exist.");
        Environment.Exit(1);
    }

    try
    {
        if (opts.Modules || (!opts.Built))
        {
            ListModules(workingDir, opts);
        }
        
        if (opts.Built)
        {
            ListBuiltModules(workingDir, opts);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"List operation failed: {ex.Message}");
        Environment.Exit(1);
    }
}

static void BuildAllModules(string workingDir, BuildOptions opts)
{
    Console.WriteLine("Building all modules...");
    
    var projectFiles = Directory.GetFiles(workingDir, "*.csproj", SearchOption.AllDirectories)
                              .Concat(Directory.GetFiles(workingDir, "*.sln", SearchOption.TopDirectoryOnly))
                              .ToList();

    if (!projectFiles.Any())
    {
        Console.WriteLine("No .NET projects or solutions found.");
        return;
    }

    foreach (var projectFile in projectFiles)
    {
        var projectName = Path.GetFileNameWithoutExtension(projectFile);
        Console.WriteLine($"Building {projectName}...");
        
        if (opts.Clean)
        {
            RunDotNetCommand($"clean \"{projectFile}\"", opts.Verbose);
        }
        
        var buildConfig = opts.Release ? "Release" : "Debug";
        RunDotNetCommand($"build \"{projectFile}\" -c {buildConfig}", opts.Verbose);
    }
}

static void BuildSpecificModule(string workingDir, string module, BuildOptions opts)
{
    Console.WriteLine($"Building module: {module}");
    
    var projectFiles = Directory.GetFiles(workingDir, "*.csproj", SearchOption.AllDirectories)
                              .Where(f => Path.GetFileNameWithoutExtension(f).Contains(module, StringComparison.OrdinalIgnoreCase))
                              .ToList();

    if (!projectFiles.Any())
    {
        Console.WriteLine($"No projects found matching module '{module}'.");
        return;
    }

    foreach (var projectFile in projectFiles)
    {
        var projectName = Path.GetFileNameWithoutExtension(projectFile);
        Console.WriteLine($"Building {projectName}...");
        
        if (opts.Clean)
        {
            RunDotNetCommand($"clean \"{projectFile}\"", opts.Verbose);
        }
        
        var buildConfig = opts.Release ? "Release" : "Debug";
        RunDotNetCommand($"build \"{projectFile}\" -c {buildConfig}", opts.Verbose);
    }
}

static void BuildCurrentProject(string workingDir, BuildOptions opts)
{
    Console.WriteLine("Building current project...");
    
    var solutionFiles = Directory.GetFiles(workingDir, "*.sln", SearchOption.TopDirectoryOnly);
    var projectFiles = Directory.GetFiles(workingDir, "*.csproj", SearchOption.TopDirectoryOnly);

    string? targetFile = solutionFiles.FirstOrDefault() ?? projectFiles.FirstOrDefault();
    
    if (targetFile == null)
    {
        Console.WriteLine("No .NET project or solution found in current directory.");
        return;
    }

    var fileName = Path.GetFileNameWithoutExtension(targetFile);
    Console.WriteLine($"Building {fileName}...");
    
    if (opts.Clean)
    {
        RunDotNetCommand($"clean \"{targetFile}\"", opts.Verbose);
    }
    
    var buildConfig = opts.Release ? "Release" : "Debug";
    RunDotNetCommand($"build \"{targetFile}\" -c {buildConfig}", opts.Verbose);
}

static void ListModules(string workingDir, ListOptions opts)
{
    var projectFiles = Directory.GetFiles(workingDir, "*.csproj", SearchOption.AllDirectories);
    var solutionFiles = Directory.GetFiles(workingDir, "*.sln", SearchOption.AllDirectories);

    var modules = new List<ModuleInfo>();

    foreach (var projectFile in projectFiles)
    {
        var projectName = Path.GetFileNameWithoutExtension(projectFile);
        var relativePath = Path.GetRelativePath(workingDir, projectFile);
        var isBuilt = IsProjectBuilt(projectFile);
        
        modules.Add(new ModuleInfo
        {
            Name = projectName,
            Type = "Project",
            Path = relativePath,
            IsBuilt = isBuilt
        });
    }

    foreach (var solutionFile in solutionFiles)
    {
        var solutionName = Path.GetFileNameWithoutExtension(solutionFile);
        var relativePath = Path.GetRelativePath(workingDir, solutionFile);
        
        modules.Add(new ModuleInfo
        {
            Name = solutionName,
            Type = "Solution",
            Path = relativePath,
            IsBuilt = false
        });
    }

    OutputModules(modules, opts.Built);
}

static void ListBuiltModules(string workingDir, ListOptions opts)
{
    var projectFiles = Directory.GetFiles(workingDir, "*.csproj", SearchOption.AllDirectories);
    var builtModules = new List<ModuleInfo>();

    foreach (var projectFile in projectFiles)
    {
        if (IsProjectBuilt(projectFile))
        {
            var projectName = Path.GetFileNameWithoutExtension(projectFile);
            var relativePath = Path.GetRelativePath(workingDir, projectFile);
            
            builtModules.Add(new ModuleInfo
            {
                Name = projectName,
                Type = "Project",
                Path = relativePath,
                IsBuilt = true
            });
        }
    }

    OutputModules(builtModules, true);
}

static bool IsProjectBuilt(string projectFile)
{
    var projectDir = Path.GetDirectoryName(projectFile);
    var binDir = Path.Combine(projectDir!, "bin");
    return Directory.Exists(binDir) && Directory.GetDirectories(binDir).Any();
}

static void OutputModules(List<ModuleInfo> modules, bool builtOnly)
{
    var filteredModules = builtOnly ? modules.Where(m => m.IsBuilt).ToList() : modules;

    if (!filteredModules.Any())
    {
        Console.WriteLine("No modules found.");
        return;
    }
}

static void RunDotNetCommand(string arguments, bool verbose)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = arguments,
        RedirectStandardOutput = !verbose,
        RedirectStandardError = !verbose,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var process = Process.Start(startInfo);
    process?.WaitForExit();

    if (process?.ExitCode != 0)
    {
        throw new Exception($"dotnet command failed with exit code {process?.ExitCode}");
    }
}

static void HandleParseError(IEnumerable<Error> errs)
{
    foreach (var error in errs)
    {
        if (error is HelpRequestedError || error is VersionRequestedError)
            return;
    }
    Environment.Exit(1);
}

public class ModuleInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsBuilt { get; set; }
}