using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding
{
    private Kim player;

    public void setPlayer(Kim player) => this.player = player;
    public List<Grid.Tile> getNeighbours(Grid.Tile tile)
    {
        Vector2Int[] neighbourDirs = {
            new(1, 0),
            new(-1, 0),
            new(0, 1),
            new(0, -1),
            new(1, 1),
            new(-1, -1),
            new(-1, 1),
            new(1, -1),
            
        };
        
        List<Grid.Tile> neighbours = new List<Grid.Tile>();
        foreach (var dir in neighbourDirs)
        {
            Grid.Tile temp = Grid.Instance.TryGetTile(new Vector2Int(tile.x + dir.x, tile.y + dir.y));

            if (temp != null)
                neighbours.Add(temp);
        }
        
        return neighbours;
    }
    
    public int getDistance(Grid.Tile tile1, Grid.Tile tile2)
    {
        int distanceX = Mathf.Abs(tile1.x - tile2.x);
        int distanceY = Mathf.Abs(tile1.y - tile2.y);
        
        if (distanceX > distanceY)
            return 14 * distanceY + 10 * (distanceX - distanceY);
        
        return 14 * distanceX + 10 * (distanceY - distanceX);
    }
    
    public bool isWithinDistance(Grid.Tile tile1, Grid.Tile tile2, int distance)
    {
        return Mathf.Abs(tile1.x - tile2.x) < distance &&
               Mathf.Abs(tile1.y - tile2.y) < distance;
    }

    void retracePath(Grid.Tile startTile, Grid.Tile targetTile)
    {
        List<Grid.Tile> path = new List<Grid.Tile>();
        Grid.Tile currentTile = targetTile;

        while (currentTile != startTile)
        {
            path.Add(currentTile);
            currentTile = currentTile.parent;
        }
        
        path.Reverse();

        Grid.Instance.path = path;
    }
    
    public bool findPath(Vector2Int targetPos)
    {
        Grid.Instance.scannedTiles.Clear();
        Grid.Tile startTile = player.getCurrentTile();
        Grid.Tile targetTile = Grid.Instance.TryGetTile(targetPos);
        
        List<Grid.Tile> openSet = new List<Grid.Tile>();
        List<Grid.Tile> closedSet = new List<Grid.Tile>();
        
        openSet.Add(startTile);

        while (openSet.Count > 0)
        {
            Grid.Tile currentTile = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentTile.fCost || openSet[i].fCost == currentTile.fCost && openSet[i].hCost < currentTile.hCost)
                    currentTile = openSet[i];
            }
            
            openSet.Remove(currentTile);
            closedSet.Add(currentTile);

            if (currentTile == targetTile)
            {
                retracePath(startTile, targetTile);
                return true;
            }

            foreach (Grid.Tile neighbour in getNeighbours(currentTile))
            {
                if (neighbour.occupied || closedSet.Contains(neighbour))
                    continue;

                int newCostNeighbour = currentTile.gCost + getDistance(currentTile, neighbour);
                if (newCostNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newCostNeighbour;
                    if (neighbour.hCost < 1000000)
                        neighbour.hCost = getDistance(neighbour, targetTile);
                    neighbour.parent = currentTile;

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                        Grid.Instance.scannedTiles = closedSet;
                    }
                }
            }
        }

        return false;
    }
}
