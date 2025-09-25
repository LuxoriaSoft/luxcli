using CommandLine;

namespace Luxoria.CLI.Models;

public class BaseOptions
{
    [Option('v', "verbose", Required = false, HelpText = "Verbose Output")]
    public bool Verbose { get; set; }

    [Option('d', "directory", Required = false, HelpText = "Root Directory (default current folder)")]
    public string? Dir { get; set; }
}

[Verb("build", HelpText = "Build modules or components")]
public class BuildOptions : BaseOptions
{
    [Option('m', "module", Required = false, HelpText = "Specific module to build")]
    public string? Module { get; set; }

    [Option('a', "all", Required = false, HelpText = "Build all modules")]
    public bool All { get; set; }

    [Option('c', "clean", Required = false, HelpText = "Clean before building")]
    public bool Clean { get; set; }

    [Option('r', "release", Required = false, HelpText = "Build in release mode")]
    public bool Release { get; set; }
}

[Verb("list", HelpText = "List modules or components")]
public class ListOptions : BaseOptions
{
    [Option('m', "modules", Required = false, HelpText = "List available modules")]
    public bool Modules { get; set; }

    [Option('b', "built", Required = false, HelpText = "List built modules only")]
    public bool Built { get; set; }
}
