namespace KohdToSVG;
using BKH.Geometry;

internal class KohdMap
{
    public const int OPEN_SQUARE = 0;
    public const int BLOCKED_SQUARE = -100;
    public const int SLOW_SQUARE = -200;

    public bool AgressivePathing { get; set; } = false; // For cleanup path.

    public Point2D EndPosition { get; set; }            // Target to pathfind to.
    public List<Point2D> EndRing = [];                  // For a Kohd node, this represents the closest we're allowed to get to the EndPosition, and the ultimate target.  
    public List<Point2D> FinalPath = [];                // The path after a successful A_Star call. 
    public int MinSteps { get; set; } = 0;              // Used to ensure we have space for subnodes.
    public Point2D StartPosition { get; set; }          // Start for a single point start.
    
    // Map min/max calculations.
    public Point2D MapMin { get { return new Point2D(_theMap.Keys.Select(k => k.X).Min(), _theMap.Keys.Select(k => k.Y).Min()); } }
    public Point2D MapMax { get { return new Point2D(_theMap.Keys.Select(k => k.X).Max(), _theMap.Keys.Select(k => k.Y).Max()); } }

    private readonly Globals _globalData;             // quasi-global config object.
    private readonly Dictionary<Point2D, (float gScore, float fScore, Point2D? parent)> _stepCounter = [];  // AStar step and path data
    private readonly Dictionary<Point2D, int> _theMap = [];     // this serves as our reference starting position for everything. 

    public KohdMap(Globals config)
    {
        _globalData = config;

        foreach(Point2D p in  from x in Enumerable.Range(0, _globalData.MAPXY)
                              from y in Enumerable.Range(0, _globalData.MAPXY)
                              select new Point2D(x, y))
        {
            MarkMap(p, OPEN_SQUARE);
        }
    }
    private static IEnumerable<Point2D> NextSteps(Point2D cursor) => cursor.GetAllNeighbors();

    private float TestStep(Point2D cursor, Point2D nextStep)
    {
        //off the map
        if (!_theMap.TryGetValue(nextStep, out int nextValue)) return -1;

        //Standard don't path here
        if (nextValue == BLOCKED_SQUARE) return -1; 

        //Don't cross a diagonal. 
        if (!cursor.IsOnGridLine(nextStep))
        {
            Point2D diag1 = new(nextStep.X, cursor.Y);
            Point2D diag2 = new(cursor.X, nextStep.Y);

            if(_theMap.TryGetValue(diag1, out int value1) && 
               _theMap.TryGetValue(diag2, out int value2) && 
               (value1 != OPEN_SQUARE || value2 != OPEN_SQUARE)) return -1;
        }

        //Made it!
        
        bool isMovingInAline = false;
        if (_stepCounter[cursor].parent is not null)
        {
            isMovingInAline = nextStep == cursor + (cursor - (Point2D)_stepCounter[cursor].parent!);
        }

        float baseStep = nextValue == OPEN_SQUARE ? 1 : nextValue + 50; //line crossing penalty

        // prefer to move in a straight line.
        if (isMovingInAline) baseStep -= 0.2f;
        
        // prefer to move NEWS
        baseStep += cursor.IsOnGridLine(nextStep) ? 0 : 0.2f;

        if (EndRing.Any(x => x == nextStep))
        {
            return baseStep;
        }



        //Figure out our steps.
        //        if (nextValue == SLOW_SQUARE)
        //{
        //try to get into the End Ring on a direct line.

        //otherwise slow squares suck. 
        //  return 50; // -SLOW_SQUARE; //invert sign 'cause SLOW_SQUARE is negative.
        //}

        // drift along with other traces. 
        if (nextStep.GetOrthogonalNeighbors().Any(x => _theMap.TryGetValue(x, out int value) && value != OPEN_SQUARE))
        {
            baseStep += AgressivePathing ? -0.2f : 50.0f;
        }

        return baseStep;
    }

    // Adds a trace to the map.
    public void LayPath(List<Point2D> path)
    {
        foreach (Point2D p in path)
        {
            int mva = MapValueAt(p);
            if(mva > -1) MarkMap(p, mva + 1);
        }
    }

    // Removes a trace from the map.
    public void StripPath(List<Point2D> path)
    {
        foreach (Point2D p in path)
        {
            int mva = MapValueAt(p);
            if (mva > -1) MarkMap(p, mva - 1);
        }
    }

    // Set a map value
    public void MarkMap(Point2D point, int value)
    {
        if(!_theMap.TryAdd(point, value))
        {   
            _theMap[point] = value;     
        }
    }

    // Get a map value
    public int MapValueAt(Point2D point)
    {
        if (_theMap.TryGetValue(point, out int value))
        {
            return value;
        }
        Console.WriteLine($"Value out of bounds. {point}");
        return -1; 
    }

    public string Debug_Grid(bool doDiag = false)
    {
        string returnValue = string.Empty;
        foreach ((Point2D p, int value) in _theMap.Where(x => x.Value != 0))
        {
            (int x, int y) = _globalData.GridToWorld(p);
            string color = value switch
            {
                BLOCKED_SQUARE => "red",
                SLOW_SQUARE => "yellow",
                1 => "green",
                2 => "orange",
                3 => "blue",
                _ => "purple"
            };

            returnValue += $"<rect x=\"{x}\" y=\"{y}\" width=\"{_globalData.GridSize}\" height=\"{_globalData.GridSize}\" style=\"fill:{color};fill-opacity:0.3\"/>\n";
        }

        if(doDiag)
        {
            for (int i = 0; i < 35; i++)
            {
                returnValue += $"<rect x=\"{i * _globalData.GridSize}\" y=\"{i * _globalData.GridSize}\" width=\"{_globalData.GridSize}\" height=\"{_globalData.GridSize}\" style=\"fill=:{(int.IsEvenInteger(i) ? "green" : "pink")};fill-opacity:0.5\"/>\n";
            }
        }
        return returnValue;
    }

    public void Debug_ConsoleMap()
    {
        for (int y = MapMin.Y; y <= MapMax.Y; y++)
        {
            for (int x = MapMin.X; x <= MapMax.X; x++)
            {
                Point2D current = new(x, y);

                if (_theMap.TryGetValue(current, out int value))
                {
                    switch (value)
                    {
                        case OPEN_SQUARE:
                            Console.Write(' ');
                            break;
                        case BLOCKED_SQUARE:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write('X');
                            break;
                        case SLOW_SQUARE:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write('.');
                            break;
                        default:
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.Write('?');
                            break;
                    }
                }
                Console.ResetColor();
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }

    private static int Heuristic(Point2D a, Point2D b) => Point2D.TaxiDistance2D(a, b);

    public bool A_Star(Node fromNode, Node toNode)
    {
        EndPosition = toNode.GridXY;
        EndRing = [.. toNode.EdgePoints];
        MinSteps = fromNode.MinTraceDistance();
        return A_Star(fromNode.EdgePoints, EndPosition);
    }

    public bool A_Star() => A_Star([StartPosition], EndPosition);

    public bool A_Star(List<Point2D> start, Point2D end)
    {
        // AStar pulled from Wikipedia psudocode.

        FinalPath.Clear();
        _stepCounter.Clear();

        PriorityQueue<Point2D, float> searchQueue = new(); //we enque based on fScore + h, the distance travelled, plus taxi distance guess to destination.
        HashSet<Point2D> inSearchQueue = []; //we add this because we don't have a way to query the queue to see if a specific item is in it.

        int gScore = 0; //gScore is value of the path from start to here
        foreach (Point2D startPos in start)
        {
            if (MapValueAt(startPos) == OPEN_SQUARE)
            {
                _stepCounter.Add(startPos, (gScore, Heuristic(startPos, end), null));

                searchQueue.Enqueue(startPos, _stepCounter[startPos].fScore);
                inSearchQueue.Add(startPos);
            }
        }

        while (searchQueue.TryDequeue(out Point2D cursor, out _))
        {
            inSearchQueue.Remove(cursor);

            //We have arrived!
            if (EndRing.Any(x => x == cursor))
            {
                FinalPath.Add(cursor);
                Point2D? p = _stepCounter[cursor].parent;

                while (p != null)
                {
                    FinalPath.Add((Point2D)p);
                    p = _stepCounter[(Point2D)p].parent;
                }

                if (FinalPath.Count >= MinSteps)    //check MinSteps for subnode spacing.
                {
                    FinalPath.Reverse();
                    return true;
                }
                FinalPath.Clear();
                continue;
            }

            foreach (Point2D nextStep in NextSteps(cursor))
            {
                float dist = TestStep(cursor, nextStep);    //bounds and valid move check. 
                if (dist == -1) continue;

                _stepCounter.TryAdd(nextStep, (int.MaxValue, int.MaxValue, null));
                float t_gScore = _stepCounter[cursor].gScore + dist;    //tentative_gScore := gScore[current] + d(current, neighbor)

                if (t_gScore < _stepCounter[nextStep].gScore)     //if tentative_gScore < gScore[neighbor]
                {
                    //cameFrom[neighbor] := current
                    //gScore[neighbor] := tentative_gScore
                    //fScore[neighbor] := tentative_gScore + h(neighbor)
                    _stepCounter[nextStep] = (t_gScore, t_gScore + Heuristic(cursor, end), cursor);

                    if (!inSearchQueue.Contains(nextStep))  //if neighbor not in openSet openSet.add(neighbor) 
                    {
                        searchQueue.Enqueue(nextStep, _stepCounter[nextStep].fScore);
                        inSearchQueue.Add(nextStep);
                    }
                }
            }
        }
        return false; // exhaustd queue without finding the exit.
    }
}