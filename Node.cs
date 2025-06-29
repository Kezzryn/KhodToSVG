using BKH.Geometry;
namespace KohdToSVG;

internal class Node((int X, int Y) worldXY, int position, int radius, Globals globals)
{
    public List<Point2D> EdgePoints = [];
    public Node? NextNode { get; set; } = null;
    public Point2D GridXY { get; set; } = globals.WorldToGrid(worldXY);

    public List<Point2D> GridPath = [];
    public bool IsCharge { get { return _chargeLinkTrace == String.Empty; } }
    public bool IsGround { get { return NextNode is null; } }
    public int POS { get; set; } = position;
    public int Radius { get; set; } = radius;
    public readonly List<int> SubNodes = [];
    public readonly List<(int X, int Y)> TraceLine = [];
    public int WorldX { get; set; } = worldXY.X;
    public int WorldY { get; set; } = worldXY.Y;

    private readonly Globals _globalData = globals;
    private string _chargeLinkTrace = String.Empty;

    public void AddChargeLinkTrace(KohdMap KohdMap)
    {
        int offset = _globalData.GridSize / 2;
        int quarterOffset = _globalData.GridSize / 4;

        //figure out what direction we're going.
        int step = EdgePoints.Min(x => Point2D.TaxiDistance2D(GridXY, x));

        Point2D.Direction dir = Point2D.Direction.Left;
        //Point2D.Direction dir = node.POS switch
        //{
        //    1 or 4 or 7 => Point2D.Direction.Left,
        //    3 or 6 or 9 => Point2D.Direction.Right,
        //    2 => Point2D.Direction.Up,
        //    5 or 8 => Point2D.Direction.Down,
        //    _ => throw new NotImplementedException($"Unknown POS {node.POS}")
        //};

        Point2D startChargePos = GridXY.OrthogonalNeighbor(dir, step + 3);
        Point2D endChargePos = GridXY.OrthogonalNeighbor(dir, step);

        List<(int x, int y)> pointList = [];

        if (dir == Point2D.Direction.Left) // one day we'll do this in any direction. This isn't that day. 
        {
            (int x, int y) cursorChargeNode = _globalData.GridToWorld(startChargePos, offset);
            pointList.Add(cursorChargeNode);
            cursorChargeNode.x += offset;
            pointList.Add(cursorChargeNode);
            cursorChargeNode.y -= _globalData.GridSize;
            cursorChargeNode.x += quarterOffset;
            pointList.Add(cursorChargeNode);

            for (int i = 0; i < 3; i++)
            {
                cursorChargeNode.y += _globalData.GridSize * 2;
                cursorChargeNode.x += quarterOffset;
                pointList.Add(cursorChargeNode);
                cursorChargeNode.y -= _globalData.GridSize * 2;
                cursorChargeNode.x += quarterOffset;
                pointList.Add(cursorChargeNode);
            }
            cursorChargeNode.y += _globalData.GridSize;
            cursorChargeNode.x += quarterOffset;
            pointList.Add(cursorChargeNode);
            cursorChargeNode.x += offset;
            pointList.Add(cursorChargeNode);

            KohdMap.MarkMap(startChargePos, KohdMap.BLOCKED_SQUARE);
            KohdMap.MarkMap(endChargePos, KohdMap.BLOCKED_SQUARE);
            foreach (Point2D p in from y in Enumerable.Range(-1, 3)
                                  from x in Enumerable.Range(1, 2)
                                  select startChargePos + new Point2D(x, y))
            {
                KohdMap.MarkMap(p, KohdMap.BLOCKED_SQUARE);
            }
        }

        if (pointList.Count > 0)
        {
            pointList.Add(Node.CalculateIntersection(WorldX, WorldY, Radius, pointList.Last()));

            string chargeNode = String.Join(" ", pointList.Select(s => $"{s.x},{s.y}"));

            _chargeLinkTrace = $"<polyline points=\"" + chargeNode + "\" style=\"fill:none;stroke:green;stroke-width:3\"/>\n";
        }
    }

    public void AddGroundLinkTrace(KohdMap KohdMap)
    {
        int offset = _globalData.GridSize / 2;
        int step = EdgePoints.Min(x => Point2D.TaxiDistance2D(GridXY, x));

        Point2D.Direction dir = Point2D.Direction.Right;
        Point2D cursor = GridXY.OrthogonalNeighbor(dir, step);

        GridPath.Add(cursor);
        cursor = cursor.OrthogonalNeighbor(Point2D.Direction.Right);
        GridPath.Add(cursor);
        cursor = cursor.OrthogonalNeighbor(Point2D.Direction.Right);
        cursor = cursor.OrthogonalNeighbor(Point2D.Direction.Up);
        GridPath.Add(cursor);

        for (int i = 0; i < MinTraceDistance() - 1; i++)
        {
            cursor = cursor.OrthogonalNeighbor(Point2D.Direction.Up);
            GridPath.Add(cursor);
        }

        cursor = cursor.OrthogonalNeighbor(Point2D.Direction.Left);
        GridPath.Add(cursor);
        cursor = cursor.OrthogonalNeighbor(Point2D.Direction.Right, 2);
        GridPath.Add(cursor);

        foreach (Point2D p in GridPath)
        {
            KohdMap.MarkMap(p, KohdMap.BLOCKED_SQUARE);
        }
    }

    public void GenerateStartPoints(int maxRadius)
    {
        //not perfect, we lose a few at the corners. Something for Future Me.
        //int n_r = ((((maxRadius  * 2) + KohdMap.GRID_SIZE - 1) / KohdMap.GRID_SIZE) - 1) / 2;
        EdgePoints = [.. GridXY.GetNeighborsAtRadius(maxRadius)];
    }

    public int MinTraceDistance() => SubNodes.Sum() + SubNodes.Count; // Count -1 for spaces, but +1 for leading space. 

    public string NodeSVG()
    {
        return DrawNode() + DrawTraceLine() + DrawSubNodes(); 
    }

    private static (int x, int y) CalculateIntersection(int sourceX, int sourceY, int sourceRadius, (int X, int Y) target)
    {
        // Circle center and radius
        double cx = sourceX;
        double cy = sourceY;
        double radius = sourceRadius;

        // Target point on the line
        double tx = target.X;
        double ty = target.Y;

        // Direction vector from center to target
        double dx = tx - cx;  // -2
        double dy = ty - cy;  // 2

        // Normalize direction vector
        double length = Math.Sqrt(dx * dx + dy * dy);
        double dxNorm = dx / length;
        double dyNorm = dy / length;

        // Move from center in that direction by radius
        double intersectionX = cx + dxNorm * radius;
        double intersectionY = cy + dyNorm * radius;
        return ((int)intersectionX, (int)intersectionY);
    }

    private string DrawNode() => $"<circle cx=\"{WorldX}\" cy=\"{WorldY}\" r=\"{Radius}\" style=\"fill:red;stroke:black;stroke-width:3;fill-opacity:0.0\"/>";

    private string DrawSubNodes()
    {
        string returnvalue = "";

        if((SubNodes.Sum() + SubNodes.Count - 1) > (TraceLine.Count - 1))
        {
            Console.WriteLine($"ERROR: Traceline is too short. ({SubNodes.Sum()} + {SubNodes.Count - 1}) >  {TraceLine.Count} ");
            return returnvalue;
        }

        int traceLinePos = 2;
        for(int i = 0; i < SubNodes.Count; i++)
        {
            for(int j = 0; j < SubNodes[i]; j++)
            {
                returnvalue += $"<circle cx=\"{TraceLine[traceLinePos].X}\" cy=\"{TraceLine[traceLinePos].Y}\" r=\"{_globalData.SubNodeRadius}\" style=\"stroke:black;stroke-width:3;fill-opacity:1.0\"/>\n";

                traceLinePos++;
            }
            if (i < SubNodes.Count - 1) traceLinePos++;
        }

        return returnvalue;
    }

    private string DrawTraceLine()
    {
        if (GridPath.Count == 0)
        {
            Console.WriteLine($"ERROR: No FinalPath for POS: {this}");
            return "";
        }

        //from node source intersect at node radius, targeting first trace line. 
        int offset = _globalData.GridSize / 2;
        TraceLine.Add(CalculateIntersection(WorldX, WorldY, Radius, _globalData.GridToWorld(GridPath.First(), offset)));

        foreach (Point2D p in GridPath)
        {
            TraceLine.Add(_globalData.GridToWorld(p, offset));
        }

        if (!IsGround) // A ground doesn't need to connect to the next node.
        { 
            TraceLine.Add(CalculateIntersection(NextNode!.WorldX, NextNode.WorldY, NextNode.Radius, _globalData.GridToWorld(GridPath.Last(), offset))); 
        }

        //TODO shorten/simplify traceline.

        string pointList = String.Join(" ", TraceLine.Select(x => $"{x.X},{x.Y}"));

        return _chargeLinkTrace + $"<polyline points=\"" + pointList + "\" style=\"fill:none;stroke:green;stroke-width:3\"/>\n";
    }

    public override string ToString() => $"{POS} R:{Radius}";
}