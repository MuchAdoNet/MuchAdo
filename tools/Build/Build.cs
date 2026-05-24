var buildSettings = new DotNetBuildSettings
{
	NuGetApiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY"),
	CoverageSettings = new DotNetCoverageSettings { TargetFramework = "net10.0" },
	PackageSettings = new DotNetPackageSettings { PushTagOnPublish = x => $"v{x.Version}" },
};

return BuildRunner.Execute(args, build =>
{
	build.AddDotNetTargets(buildSettings);

	build.Target("test-no-docker")
		.DependsOn("build")
		.Describe("Runs tests that do not require Docker")
		.Does(() =>
		{
			var dockerTestProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"MuchAdo.MySql.Tests.csproj",
				"MuchAdo.Npgsql.Tests.csproj",
				"MuchAdo.SqlServer.Tests.csproj",
			};

			foreach (var project in FindFiles("tests/**/*.csproj")
				.Where(project => !dockerTestProjects.Contains(Path.GetFileName(project)))
				.Order(StringComparer.OrdinalIgnoreCase))
			{
				RunDotNet(
				[
					"test",
					project,
					"-c",
					buildSettings.GetConfiguration(),
					buildSettings.GetPlatformArg(),
					buildSettings.GetBuildNumberArg(),
					"--no-build",
					buildSettings.GetVerbosityArg(),
					buildSettings.GetMaxCpuCountArg(),
					.. buildSettings.GetExtraPropertyArgs("test"),
				]);
			}
		});
});
