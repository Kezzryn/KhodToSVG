namespace KhodToSVG;
using BKH.Geometry;

internal class Globals
{
    public Globals(CommandLineParser clp)
    {
        Verbose = clp.GetSwitchArgument("verbose", 'v');
        DebugGrid = clp.GetSwitchArgument("debuggrid", 'd');

        string configFile = clp.GetStringArgument("configfile", 'c');
        if(configFile == String.Empty) 
        {
            configFile = "khod.config";
            Console.WriteLine("Using default config file.");
        }

        if (File.Exists(configFile))
        {
            if (Verbose) Console.WriteLine($"Loading config from: {configFile}");
        }
        //else
        //{
        //    Console.WriteLine($"WARNING: {configFile} not found. Using hardcoded defaults.");
        //}
    }

    //Global
    public string CacheDirectory { get; } = @"cache\";
    public bool DebugGrid { get; } = false;
    public string DefaultOutput { get; } = "khod.html";
    public bool Verbose { get; } = false;
    public bool UseCache { get; } = false;
    public bool EmbedSVG { get; } = true;
    public bool NotSilent { get; } = true;

    //default file names
    //word cache directory

    //KhodWord
    public int RADII_INCREASE = 7;
    public int DEFAULT_RADIUS = 20;
    // style sheets? 
    // 15 px
    //const int SPACING = 150;
    //const int MARGIN_X = 127;
    //const int MARGIN_Y = 127;
    //const int MAPXY = 35;

    // 9px 
    public int SPACING { get; }  = 153;
    public int MARGIN_X { get; } = 130;
    public int MARGIN_Y { get; } = 130;
    public int MAPXY { get; } = 62;

    //KhodMap
    public int GridSize = 9;

    //Node
    public int SubNodeRadius = 3;
    //Conversions
    public (int x, int y) GridToWorld(Point2D p, int offset = 0)
    {
        int x = (p.X * GridSize) + offset;
        int y = (p.Y * GridSize) + offset;

        return (x, y);
    }

    public Point2D WorldToGrid((int x, int y) world)
    {
        return WorldToGrid(world.x, world.y);
    }

    public Point2D WorldToGrid(int x, int y)
    {
        int newX = (x - (GridSize / 2)) / GridSize;
        int newY = (y - (GridSize / 2)) / GridSize;

        Point2D p = new(newX, newY);

        return p;
    }
}
