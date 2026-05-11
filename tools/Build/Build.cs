var buildSettings = Build.CreateSettings();

return BuildRunner.Execute(args, build =>
{
	build.AddDotNetTargets(buildSettings);
	Build.AddTargets(build, buildSettings);
});

internal static class Build
{
	public static DotNetBuildSettings CreateSettings()
	{
		DotNetBuildSettings? settings = null;
		settings = new DotNetBuildSettings
		{
			NuGetApiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY"),
			TestSettings = new DotNetTestSettings
			{
				FindProjects = FindUnitTestProjects,
				RunTests = path => RunUnitTestProject(settings!, path),
			},
		};

		return settings;
	}

	public static void AddTargets(BuildApp build, DotNetBuildSettings settings)
	{
		build.Target("test-docker")
			.DependsOn("build")
			.Describe("Runs Docker-backed database tests")
			.Does(() => RunDockerTests(settings));
	}

	private static IReadOnlyList<string> FindUnitTestProjects() =>
		[.. FindFiles("tests/**/*.csproj").Where(project => !IsDockerTestProject(project))];

	private static void RunUnitTestProject(DotNetBuildSettings settings, string? path)
	{
		if (path is not null && IsDockerTestProject(path))
		{
			Console.WriteLine($"Skipping Docker-backed test project {path}.");
			return;
		}

		RunTestProject(settings, path, framework: null, filter: "TestCategory!=Docker", logger: null, resultsDirectory: null);
	}

	private static void RunDockerTests(DotNetBuildSettings settings)
	{
		Directory.CreateDirectory(c_testResultsDirectory);

		RunDockerCompose("up", "-d", "--build", "mssql", "mysql", "postgres");
		RunDockerCompose("build", "setup");

		var completed = false;
		try
		{
			RunDockerCompose("run", "--rm", "setup");

			foreach (var project in s_dockerTestProjects)
			{
				var projectName = Path.GetFileNameWithoutExtension(project);
				RunTestProject(
					settings,
					project,
					framework: "net10.0",
					filter: "TestCategory=Docker",
					logger: $"trx;LogFileName={projectName}.trx",
					resultsDirectory: c_testResultsDirectory);
			}

			completed = true;
		}
		catch
		{
			WriteDockerLogs();
			throw;
		}
		finally
		{
			try
			{
				RunDockerCompose("down", "-v");
			}
			catch when (!completed)
			{
			}
		}
	}

	private static void RunTestProject(DotNetBuildSettings settings, string? path, string? framework, string? filter, string? logger, string? resultsDirectory)
	{
		var arguments = new List<string?>
		{
			"test",
			path,
			"-c",
			settings.GetConfiguration(),
			settings.GetPlatformArg(),
			settings.GetBuildNumberArg(),
			"--no-build",
			settings.GetVerbosityArg(),
			settings.GetMaxCpuCountArg(),
		};

		arguments.AddRange(settings.GetExtraPropertyArgs("test"));

		if (framework is not null)
		{
			arguments.Add("--framework");
			arguments.Add(framework);
		}

		if (filter is not null)
		{
			arguments.Add("--filter");
			arguments.Add(filter);
		}

		if (resultsDirectory is not null)
		{
			arguments.Add("--results-directory");
			arguments.Add(resultsDirectory);
		}

		if (logger is not null)
		{
			arguments.Add("--logger");
			arguments.Add(logger);
		}

		arguments.Add("--");
		arguments.Add("RunConfiguration.TreatNoTestsAsError=true");

		RunDotNet(arguments);
	}

	private static void RunDockerCompose(params string?[] args) =>
		RunApp("docker", new[] { "compose", "-f", c_composeFile }.Concat(args));

	private static void WriteDockerLogs()
	{
		Directory.CreateDirectory(c_testResultsDirectory);

		using var writer = File.CreateText(c_dockerLogPath);
		RunApp("docker", new AppRunnerSettings
		{
			Arguments = new[] { "compose", "-f", c_composeFile, "logs", "--no-color" },
			HandleErrorLine = writer.WriteLine,
			HandleOutputLine = writer.WriteLine,
			IsExitCodeSuccess = exitCode => true,
		});
	}

	private static bool IsDockerTestProject(string path)
	{
		var normalizedPath = NormalizePath(path);
		return s_dockerTestProjects.Any(project => normalizedPath.EndsWith(project, StringComparison.OrdinalIgnoreCase));
	}

	private static string NormalizePath(string path) => path.Replace('\\', '/');

	private const string c_composeFile = "docker/docker-compose.yml";
	private const string c_testResultsDirectory = "release/TestResults";
	private const string c_dockerLogPath = "release/TestResults/docker-compose.log";

	private static readonly string[] s_dockerTestProjects =
	[
		"tests/MuchAdo.MySql.Tests/MuchAdo.MySql.Tests.csproj",
		"tests/MuchAdo.Npgsql.Tests/MuchAdo.Npgsql.Tests.csproj",
		"tests/MuchAdo.SqlServer.Tests/MuchAdo.SqlServer.Tests.csproj",
	];
}
