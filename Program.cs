using KohdToSVG;
using System.Diagnostics;
using System.Text;

static void DisplayHelp()
{
    Console.WriteLine("This program is mostly helpless.");
    Console.WriteLine("-h Help");
    Console.WriteLine("-f \"Filename\"");
    Console.WriteLine("-t \"Hello World\"");
    Console.WriteLine("-v verbose mode");
    Console.WriteLine("-s enable the stopwatch");
    Console.WriteLine();
}

//Check for and strip non letter characters 
//lowercase everything 
static string ValidateText(string text) => new string([.. text.Where(c => (char.IsLetter(c) || char.IsWhiteSpace(c)))]).ToLower();

Dictionary<string, string> KohdCache = [];

try
{
    CommandLineParser clp = new(args);
    Globals globalData = new(clp);

    //Do initalizaton stuff.
    DirectoryInfo dirInfo = Directory.CreateDirectory(globalData.CacheDirectory);
    foreach(FileInfo fi in dirInfo.EnumerateFiles())
    {
        KohdCache.Add(Path.GetFileNameWithoutExtension(fi.Name), fi.FullName);
    }

    if(args.Length == 0) DisplayHelp();

    if (clp.GetSwitchArgument("help", 'h'))
    {
        DisplayHelp();
        return;
    }

    string fileName = clp.GetStringArgument("file", 'f');
    if(fileName == String.Empty)
    {
        Console.WriteLine($"No output file specifed, defaulting to {globalData.DefaultOutput}\n");
        fileName = globalData.DefaultOutput;
    }
    
    string inputText = clp.GetStringArgument("text", 't');
    if (inputText == String.Empty)
    {
        Console.WriteLine("No text to translate specifed.");
        Console.WriteLine("Enter text and press enter, or just press enter to exit.");
        inputText = Console.ReadLine() ?? String.Empty;
        if (inputText == String.Empty) return; 
    }
    inputText = ValidateText(inputText);


    bool stopWatch = clp.GetSwitchArgument("stopwatch", 's');
    Stopwatch sw = new();
    if (stopWatch) { sw.Start(); }

    List<string> splitText = [.. inputText.Split(" ", StringSplitOptions.RemoveEmptyEntries)];

    IEnumerable<string> toBeTranslated = splitText.Distinct();

    if (globalData.UseCache)
    {
        toBeTranslated = toBeTranslated.Except(KohdCache.Keys);
    }
  
    IEnumerable<KohdWord> translated = toBeTranslated.Select(x => new KohdWord(x, globalData)).AsParallel();

    foreach(KohdWord s in translated)
    {
        KohdCache.TryAdd(s.SourceWord, $"{globalData.CacheDirectory}{s.SourceWord}.svg");

        File.WriteAllText(KohdCache[s.SourceWord], s.ToString());
    }

    const string HTML_HEADER = "<!DOCTYPE html>\n<html>\n<body>\n";
    const string HTML_FOOTER = "</body>\n</html>";
    // check for existing files. 

    StringBuilder sb = new StringBuilder();
    sb.Append(HTML_HEADER);

    foreach(string s in splitText)
    {
        if(KohdCache.TryGetValue(s, out string fname))
        {
            sb.Append(File.ReadAllText(fname));
        }
    }

    sb.Append(HTML_FOOTER);

    File.WriteAllText(fileName, sb.ToString());

    if (stopWatch)
    {
        sw.Stop();
        Console.WriteLine($"Completed in {sw.ElapsedMilliseconds} ms");
    }
}
catch (Exception e)
{
    Console.WriteLine(e);
}