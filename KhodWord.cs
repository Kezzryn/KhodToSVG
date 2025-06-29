using BKH.Geometry;
namespace KhodToSVG;

internal class KhodWord
{
    private static readonly Dictionary<int, List<char>> pos_to_char = new()
    {
        { 1, ['a','b','c'] },
        { 2, ['d','e','f'] },
        { 3, ['g','h','i'] },
        { 4, ['j','k','l'] },
        { 5, ['m','n','o'] },
        { 6, ['p','q','r'] },
        { 7, ['s','t','u'] },
        { 8, ['v','w','x'] },
        { 9, ['y','z'] }
    };

    private static readonly Dictionary<char, int> char_to_pos = new()
    {
        { 'a', 1 },     { 'd', 2 },     { 'g', 3 },
        { 'b', 1 },     { 'e', 2 },     { 'h', 3 },
        { 'c', 1 },     { 'f', 2 },     { 'i', 3 },

        { 'j', 4 },     { 'm', 5 },     { 'p', 6 },
        { 'k', 4 },     { 'n', 5 },     { 'q', 6 },
        { 'l', 4 },     { 'o', 5 },     { 'r', 6 },

        { 's', 7 },     { 'v', 8 },     { 'y', 9 },
        { 't', 7 },     { 'w', 8 },     { 'z', 9 },
        { 'u', 7 },     { 'x', 8 },
    };

    private static readonly Dictionary<int, Dictionary<int, int>> distance = [];

    private static readonly Dictionary<Point2D, int> point_to_pos = new()
    {
        { new(-1, -1), 1},
        { new(-1,  0), 2},
        { new(-1,  1), 3},
        { new( 0, -1), 4},
        { new( 0,  0), 5},
        { new( 0,  1), 6},
        { new( 1, -1), 7},
        { new( 1,  0), 8},
        { new( 1,  1), 9}
    };

    private readonly List<Node> nodes = [];

    private readonly KhodMap baseKhodMap;

    public string SourceWord { get; }

    private readonly Globals _globalData;

    public KhodWord(string text, Globals globals)
    {
        _globalData = globals;

        SourceWord = text;

        if (_globalData.Verbose) Console.WriteLine($"Beginning parse of {text}");
        baseKhodMap = new(globals);

        foreach ((Point2D source, int sourcePos) in point_to_pos)
        {
            distance.TryAdd(sourcePos, []);
            foreach((Point2D dest, int destPos) in point_to_pos.Where(x => x.Key != source))
            {
                int dist = Point2D.TaxiDistance2D(source, dest);
                distance[sourcePos].TryAdd(destPos, dist);
            }
        }

        if (_globalData.Verbose) Console.WriteLine($"- Setting up nodes.");
        SetupNodes(text);
        if (_globalData.Verbose) Console.WriteLine($"- Starting Link Tracing.");
        CalcLinkTrace();
    }

    public void CalcLinkTrace()
    {
        if (_globalData.Verbose) Console.WriteLine("-- Build pairs of start and target nodes");

        for (int toNode = 1; toNode < nodes.Count; toNode++)
        {
            int fromNode = toNode - 1;
            if (_globalData.Verbose) Console.WriteLine($"-- Adding link from node# {nodes[fromNode].POS} to {nodes[toNode].POS}");
            
            nodes[fromNode].NextNode = nodes[toNode];
            //            nodes[fromNode].SortStartPoints(nodes[toNode].GridXY);
        }

        //build charge
        Node firstLink = nodes.First();
        firstLink.AddChargeLinkTrace(baseKhodMap);

        //build a ground.
        Node lastLink = nodes.Last();
        lastLink.AddGroundLinkTrace(baseKhodMap);

        foreach (Node currentNode in nodes.Where(x => !x.IsGround).OrderBy(x => distance[x.POS][x.NextNode!.POS]))
        {
            //run each path on it's own.
            if (baseKhodMap.A_Star(currentNode, currentNode.NextNode!))
            {
                currentNode.GridPath = [.. baseKhodMap.FinalPath]; //make backup copy for possible unmarking later.
                if (_globalData.Verbose) Console.WriteLine($"-- FinalPath found. {currentNode.GridPath.First()} to {currentNode.GridPath.Last()}");
            }
            else
            {
                //this should never happen.
                Console.WriteLine($"Error no base path found for {currentNode.POS} to {currentNode.NextNode!.POS}");
            }
        }
        //now that we have all the base paths, lay them on the map. 
        foreach (Node node in nodes.SkipLast(1))
        {
            baseKhodMap.LayPath(node.GridPath);
        }
        
        bool isDone;
        int saftey = 0;
        do
        {
            saftey++;
            isDone = true;

            foreach (Node node in nodes.Where(x => !x.IsGround))
            {
                if (_globalData.Verbose) Console.WriteLine($" Testing: {node} to {(node.IsGround ? "Ground" : node.NextNode!)}");
                if (node.GridPath.Any(x => baseKhodMap.MapValueAt(x) != 1))
                {
                    //if (Verbose) Console.WriteLine($"--Cross Path!");
                    //try to do better
                    if(baseKhodMap.A_Star(node, node.NextNode!))
                    {
                        baseKhodMap.StripPath(node.GridPath);
                        node.GridPath = [.. baseKhodMap.FinalPath];
                        baseKhodMap.LayPath(node.GridPath);
                        isDone = false;
                    }
                }
            }

            if (saftey > 1000)
            {
                if (_globalData.Verbose) Console.WriteLine("Safety triggered");
                isDone = true;
            }
        } while (!isDone);

        //do cleanup step.
        baseKhodMap.AgressivePathing = true;
        for (int i = 0; i < 5; i++)
        {
            foreach (Node node in nodes.Where(x => !x.IsGround).OrderBy(x => x.GridPath.Count))
            {
                baseKhodMap.StripPath(node.GridPath);
                if (baseKhodMap.A_Star(node, node.NextNode!))
                {
                    node.GridPath = [.. baseKhodMap.FinalPath];
                    baseKhodMap.LayPath(node.GridPath);
                    isDone = false;
                }
            }
        }
    }

    private (int x, int y) NodePosition(int nodeNum)
    {
        int x = (((nodeNum - 1) % 3) * _globalData.SPACING) + _globalData.MARGIN_X;
        int y = (((nodeNum - 1) / 3) * _globalData.SPACING) + _globalData.MARGIN_Y;

        return (x, y);
    }

    private void SetupNodes(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if(!char_to_pos.TryGetValue(text[i], out int currNodePos))
            {
                if(_globalData.NotSilent) Console.WriteLine($"Unable to translate character: '{text[i]}'");
                continue;
            }
            
            int nodeRadius = _globalData.DEFAULT_RADIUS;

            if (nodes.Any(x => x.POS == currNodePos))
            {
                nodeRadius = nodes.Where(x => x.POS == currNodePos).Max(x => x.Radius) + _globalData.RADII_INCREASE;
            }

            Node newNode = new(NodePosition(currNodePos), currNodePos, nodeRadius, _globalData);
            for (int j = i; j < text.Length; j++)
            {
                if (char_to_pos[text[j]] == currNodePos)
                {
                    newNode.SubNodes.Add(pos_to_char[currNodePos].IndexOf(text[j]) + 1);
                    if (j == text.Length - 1)
                    {
                        i = j;
                        break;
                    }
                }
                else
                {
                    i = j - 1;
                    break;
                }
            }

            nodes.Add(newNode);
        }

        //now that we have all the nodes setup, we need to update our map, and then push StartPoints back on each node at each POS.
        foreach(int pos in nodes.Select(x => x.POS).Distinct())
        {
            Point2D gridNode = _globalData.WorldToGrid(NodePosition(pos));
            baseKhodMap.MarkMap(gridNode, KhodMap.BLOCKED_SQUARE);

            int maxRadius = nodes.Where(x => x.POS == pos).Max(r => r.Radius);
            int n_r = ((((maxRadius * 2) + _globalData.GridSize - 1) / _globalData.GridSize) - 1) / 2;
            foreach (Point2D n in gridNode.GetAllNeighbors(n_r))
            {
                baseKhodMap.MarkMap(n, KhodMap.BLOCKED_SQUARE);
            }

            //List<Point2D> startPoints = [.. gridNode.GetNeighborsAtRadius(n_r + 1)];
            //foreach (Point2D n in startPoints)
            //{
            //    baseKhodMap.MarkMap(n, KhodMap.SLOW_SQUARE);
            //}

            foreach (Node n in nodes.Where(x => x.POS == pos))
            {
                n.GenerateStartPoints(n_r + 1);
            }
        }
    }

    public override string ToString()
    {
        string grid = String.Empty;
        if(_globalData.DebugGrid) grid = baseKhodMap.Debug_Grid();

        string body = string.Join("\n", nodes.Select(x => x.NodeSVG()));
        string nullNode = String.Empty;

        const string SVG_HEADER = "<svg height=\"600\" width=\"600\" xmlns=\"http://www.w3.org/2000/svg\">\n";
        const string SVG_FOOTER = "</svg>\n"; //</g>\n

        return SVG_HEADER + grid + body + nullNode + SVG_FOOTER;
    }
}