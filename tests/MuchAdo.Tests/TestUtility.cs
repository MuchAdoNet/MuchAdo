namespace MuchAdo.Tests;

internal static class TestUtility
{
	public static IEnumerable<(string Name, object? Value)> EnumeratePairs(this SqlParamSource parameters) =>
		parameters.Enumerate().Select(x => (x.Name, x.Value));
}
