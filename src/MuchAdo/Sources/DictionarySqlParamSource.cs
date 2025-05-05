namespace MuchAdo.Sources;

internal sealed class DictionarySqlParamSource<T>(IEnumerable<KeyValuePair<string, T>> pairs) : SqlParamSource
{
	internal override void SubmitParameters(ISqlParamTarget target)
	{
		foreach (var pair in pairs)
			target.AcceptParameter(pair.Key, pair.Value, type: null);
	}
}
