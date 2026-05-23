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
				List<string?> testArguments =
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
				];

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
			const string coverageTestResultsDirectory = "artifacts/Coverage/TestResults";
			const string coverageHistoryDirectory = "artifacts/Coverage/History";

			static void CleanDirectory(string path)
			{
				if (Directory.Exists(path))
				{
					Directory.Delete(path, recursive: true);
				}

				Directory.CreateDirectory(path);
			}

			CleanDirectory(coverageTestResultsDirectory);
			CleanDirectory(coverageReportDirectory);

			foreach (var project in FindFiles("tests/**/*.csproj").Order(StringComparer.OrdinalIgnoreCase))
			{
				var projectName = Path.GetFileNameWithoutExtension(project);
				List<string?> testArguments =
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
				];

				RunDotNet(testArguments);
			}

			RunDotNet(
			[
				"dnx",
				"dotnet-reportgenerator-globaltool",
				"--yes",
				$"-reports:{coverageTestResultsDirectory}/*/coverage.cobertura.xml",
				$"-targetdir:{coverageReportDirectory}",
				$"-historydir:{coverageHistoryDirectory}",
				"-reporttypes:Html;Cobertura;TextSummary;MarkdownDeltaSummary",
				"-assemblyfilters:+MuchAdo*;-*.Tests",
			]);

			var textSummaryPath = Path.Combine(coverageReportDirectory, "Summary.txt");
			if (File.Exists(textSummaryPath))
			{
				Console.WriteLine(File.ReadAllText(textSummaryPath));
			}

			var githubStepSummaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
			var markdownDeltaSummaryPath = Path.Combine(coverageReportDirectory, "DeltaSummary.md");
			if (!string.IsNullOrWhiteSpace(githubStepSummaryPath) && File.Exists(markdownDeltaSummaryPath))
			{
				File.AppendAllText(githubStepSummaryPath, File.ReadAllText(markdownDeltaSummaryPath));
				File.AppendAllText(githubStepSummaryPath, Environment.NewLine);
			}

			Console.WriteLine($"Coverage report: {Path.GetFullPath(coverageReportDirectory)}");
		});
});
