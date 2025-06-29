namespace KohdToSVG;
internal class CommandLineParser(string[] args)
{
    // https://stackoverflow.com/questions/43232740/parse-command-line-arguments-options-in-c-sharp

    private readonly List<string> _args = [.. args];

    public string GetStringArgument(string key, char shortKey)
    {
        int index = _args.IndexOf("--" + key);

        if (index >= 0 && _args.Count > index)
        {
            return _args[index + 1];
        }

        index = _args.IndexOf("-" + shortKey);

        if (index >= 0 && _args.Count > index)
        {
            return _args[index + 1];
        }

        return String.Empty;
    }

    public bool GetSwitchArgument(string value, char shortKey)
    {
        return _args.Contains("--" + value) || _args.Contains("-" + shortKey);
    }
}