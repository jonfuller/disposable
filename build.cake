#addin nuget:?package=Cake.ArgumentHelpers
#addin nuget:?package=dotenv.net
#tool "nuget:?package=xunit.runner.console"


using dotenv.net;

DotEnv.Config(throwOnError: false);

var target = Argument("target", "default");
var configuration = Argument("configuration", "Release");

var solution = File("src/Disposing.sln");

var nugetApiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY");
var nugetSource = Environment.GetEnvironmentVariable("NUGET_SOURCE");

Task("clean")
    .Description("Calls msbuild with 'Clean' target for the solution. Accepts 'configuration' => Debug|Release")
    .Does(() =>
    {
        MSBuild(solution, settings => settings.SetConfiguration(configuration)
            .UseToolVersion(MSBuildToolVersion.Default)
            .WithTarget("Clean"));
    });

Task("build")
    .Description("Calls msbuild with 'Build' target for the solution. Accepts 'configuration' => Debug|Release")
    .Does(() =>
    {
        MSBuild(solution, settings => settings.SetConfiguration(configuration)
            .UseToolVersion(MSBuildToolVersion.Default)
            .WithTarget("build"));
    });

Task("test")
    .Description("Runs tests")
    .Does(() => DotNetCoreTest("./src/Disposing.Tests/Disposing.Tests.csproj"));

Task("rebuild")
    .Description("Calls 'clean' then 'build'")
    .IsDependentOn("clean")
    .IsDependentOn("build");

Task("restore-nuget")
    .Description("Restores NuGet packages")
    .Does(() => NuGetRestore(solution));

Task("package-build")
    .Description("Builds the NuGet package")
    .IsDependentOn("restore-nuget")
    .Does(() =>
    {
        MSBuild(solution, settings => settings.SetConfiguration("Release")
            .UseToolVersion(MSBuildToolVersion.Default)
            .WithTarget("pack"));
    });

Task("package-push")
    .Description("Pushes NuGet package to NuGet")
    .Does(() =>
    {
        var version = XmlPeek("src/Disposing/Disposing.csproj", "/Project/PropertyGroup/Version");
        var package = $"./src/Disposing/artifacts/Disposing.{version}.nupkg";

        NuGetPush(package, new NuGetPushSettings {
            Source = nugetSource,
            ApiKey = nugetApiKey
        });
     });

Task("default")
    .IsDependentOn("rebuild");

RunTarget(target);
