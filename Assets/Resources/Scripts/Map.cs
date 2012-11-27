using System;
using System.Xml;
using System.Collections.Generic;

using UnityEngine;

public class Map
{
    public string Name;
    public string Brief1;
    public string Brief2;
    public int Width;
    public int Height;
    public bool[] Cover;
    public bool[] EntMap;

    public Map(XmlDocument doc)
    {
        Name = doc["mission"]["name"].InnerText;
        Brief1 = doc["mission"]["brief1"].InnerText;
        Brief2 = doc["mission"]["brief2"].InnerText;

        var sizeString = doc["mission"]["size"].InnerText.Split(',');
        Width = int.Parse(sizeString[0]);
        Height = int.Parse(sizeString[1]);

        Cover = new bool[Width * Height];
        var coverString = doc["mission"]["map"]["cover"].InnerText;
        var coverBools = coverString.Split(',');

        for (int y = Height - 1; y >= 0; y--)
        {
            for (int x = 0; x < Width; x++)
            {
                var tile = coverBools[y * Width + x];
                Cover[(Height - 1 - y) * Width + x] = (tile.Contains("y")) ? true : false;
            }
        }

        EntMap = new bool[Width * Height];
    }

    public bool IsCover(Vector2 check)
    {
        return Cover[(int)check.y * Width + (int)check.x];
    }

    public bool HasEntity(Vector2 check)
    {
        return EntMap[(int)check.y * Width + (int)check.x];
    }

    public bool IsInBounds(Vector2 check)
    {
        return (check.x >= 0 && check.y >= 0 &&
            check.x < Width && check.y < Height);
    }

    public void DebugCoverMap()
    {
        for (int y = 0; y < Height; y++)
        {
            string debugLine = "";
            for (int x = 0; x < Width; x++)
                debugLine += (Cover[y * Width + x]) ? 'y' : 'n';
            Debug.Log(debugLine);
        }
    }

    public void UpdateEntities(List<Entity> ents)
    {
        for (int i = 0; i < Width * Height; i++)
            EntMap[i] = false;

        foreach (var e in ents)
        {
            if (e.Name != "Player")
                continue;

            EntMap[(int)e.Position.y * Width + (int)e.Position.x] = true;
        }
    }

    #region DIJKSTRAS ALGORITHM
    private class PathNode
    {
        public float dist;
        public Vector2 pos;
    }

    // HERE BE DRAGONS
    public List<Vector2> GetPath(Vector2 start, Vector2 end)
    {
        var master = new List<PathNode>();
        var visited = new List<PathNode>();
        var unvisited = new List<PathNode>();
        PathNode curr = new PathNode();

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var nodePos = new Vector2(x, y);
                if (this.IsCover(nodePos) || HasEntity(nodePos))
                    continue;

                var n = new PathNode
                {
                    pos = nodePos,
                    dist = float.PositiveInfinity,
                };
                master.Add(n);

                if (nodePos == start)
                {
                    curr = n;
                    curr.dist = 0;
                }
            }
        }

        foreach (var n in master)
            if (n.pos != curr.pos)
                unvisited.Add(n);

        int i = 0;
        while (i < 500) // failsafe
        {
            i++;
            var nbs = GetNeighboursOf(curr, unvisited);
            foreach (var n in nbs)
            {
                var dist = curr.dist + 1;
                if (dist < n.dist)
                    n.dist = dist;
            }

            visited.Add(curr);
            unvisited.Remove(curr);

            float shortest = float.PositiveInfinity;
            foreach (var n in unvisited)
            {
                if (n.dist < shortest)
                {
                    shortest = n.dist;
                    curr = n;
                }
            }

            if (curr.pos == end)
                break;
        }

        // go through visited nodes from end to start
        var path = new List<Vector2>();

        while (curr.pos != start)
        {
            path.Add(curr.pos);

            float shortest = float.PositiveInfinity;
            var nbs = GetNeighboursOf(curr, visited);
            foreach (var n in nbs)
            {
                if (n.dist < shortest)
                {
                    shortest = n.dist;
                    curr = n;
                }
            }
        }

        path.Reverse();
        return path;
    }

    private List<PathNode> GetNeighboursOf(PathNode node, List<PathNode> nodes)
    {
        var neighbours = new List<PathNode>();

        foreach (var n in nodes)
        {
            if (Vector2.Distance(n.pos, node.pos) == 1)
                neighbours.Add(n);
        }

        return neighbours;
    }

    #endregion

    public bool HasVision(Vector2 from, Vector2 to)
    {
        return (HasVisionImpl(from, to) && HasVisionImpl(to, from));
    }

    private bool HasVisionImpl(Vector2 from, Vector2 to)
    {
        // Bresenham's line algorithm
        // me <- standing on the shoulders of giants

        int sx = -1;
        int sy = -1;

        var dx = Mathf.Abs(to.x - from.x);
        var dy = Mathf.Abs(to.y - from.y);
        var curr = from;

        if (from.x < to.x)
            sx = 1;
        if (from.y < to.y)
            sy = 1;

        var err = dx - dy;

        while (true)
        {
            if (this.IsCover(curr))
                return false;

            if (curr == to)
                break;

            var e2 = err * 2;
            if (e2 > -dy)
            {
                err = err - dy;
                curr.x += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                curr.y += sy;
            }
        }

        return true;
    }
}
