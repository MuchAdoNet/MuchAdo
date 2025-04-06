return BuildRunner.Execute(args, build =>
{
	build.AddDotNetTargets(
		new DotNetBuildSettings
		{
			NuGetApiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY"),
		});
});
