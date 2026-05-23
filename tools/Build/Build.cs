var buildSettings = new DotNetBuildSettings
{
	NuGetApiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY"),
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
				var testArguments = new List<string?>
				{
					"test",
					project,
					"-c",
					buildSettings.GetConfiguration(),
					buildSettings.GetPlatformArg(),
					buildSettings.GetBuildNumberArg(),
					"--no-build",
					buildSettings.GetVerbosityArg(),
					buildSettings.GetMaxCpuCountArg(),
				};

				testArguments.AddRange(buildSettings.GetExtraPropertyArgs("test"));
				testArguments.AddRange(
				[
					"--filter",
					"TestCategory!=Docker",
				]);

				RunDotNet(testArguments);
			}
		});

	build.Target("coverage")
		.DependsOn("build")
		.Describe("Runs all tests with Coverlet and generates coverage reports")
		.Does(() =>
		{
			const string coverageReportDirectory = "artifacts/Coverage/Report";
			const string coverageRunSettings = "coverage.runsettings";
			const string coverageTestResultsDirectory = "artifacts/TestResults/Coverage";

			if (Directory.Exists(coverageTestResultsDirectory))
			{
				Directory.Delete(coverageTestResultsDirectory, recursive: true);
			}

			if (Directory.Exists(coverageReportDirectory))
			{
				Directory.Delete(coverageReportDirectory, recursive: true);
			}

			Directory.CreateDirectory(coverageTestResultsDirectory);

			foreach (var project in FindFiles("tests/**/*.csproj").Order(StringComparer.OrdinalIgnoreCase))
			{
				var projectName = Path.GetFileNameWithoutExtension(project);
				var testArguments = new List<string?>
				{
					"test",
					project,
					"-c",
					buildSettings.GetConfiguration(),
					buildSettings.GetPlatformArg(),
					buildSettings.GetBuildNumberArg(),
					"--no-build",
					buildSettings.GetVerbosityArg(),
					buildSettings.GetMaxCpuCountArg(),
				};

				testArguments.AddRange(buildSettings.GetExtraPropertyArgs("test"));
				testArguments.AddRange(
				[
					"--framework",
					"net10.0",
					"--results-directory",
					coverageTestResultsDirectory,
					"--logger",
					$"trx;LogFileName={projectName}.coverage.trx",
					"--collect",
					"XPlat Code Coverage",
					"--settings",
					coverageRunSettings,
					"--",
					"RunConfiguration.TreatNoTestsAsError=true",
				]);

				RunDotNet(testArguments);
			}

			Directory.CreateDirectory(coverageReportDirectory);
			RunDotNet(
			[
				"dnx",
				"dotnet-reportgenerator-globaltool",
				"--yes",
				$"-reports:{coverageTestResultsDirectory}/*/coverage.cobertura.xml",
				$"-targetdir:{coverageReportDirectory}",
				"-reporttypes:Html;Cobertura;MarkdownSummaryGithub",
				"-assemblyfilters:+MuchAdo*;-*.Tests",
			]);

			Console.WriteLine($"Coverage report: {Path.GetFullPath(coverageReportDirectory)}");
		});
});
