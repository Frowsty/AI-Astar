using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

public class Kim : CharacterController
{
    [SerializeField] float ContextRadius = 40f;
    private Grid.Tile startTile;
    public Burger[] Burgers;
    private List<Grid.Tile> Targets = new List<Grid.Tile>();
    private List<Zombie> Zombies = new List<Zombie>();
    private Zombie closestZombie;
    public bool shouldUpdatePath = true;
    public int TargetIndex = 0;
    private List<Grid.Tile> path = new List<Grid.Tile>();

    public override void StartCharacter()
    {
        base.StartCharacter();

        ContextRadius = 40f;
        
        Targets.Add(Grid.Instance.GetClosest(Burgers[0].transform.position));
        Targets.Add(Grid.Instance.GetClosest(Burgers[1].transform.position));
        Targets.Add(Grid.Instance.GetFinishTile());

        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Zombie"))
        {
            Zombie zombie = obj.GetComponent<Zombie>();
            
            if (zombie != null)
                Zombies.Add(zombie);
        }
        
        closestZombie = GetClosest(GetContextByTag("Zombie"))?.GetComponent<Zombie>();

        startTile = myCurrentTile;
    }
    
    public List<Grid.Tile> GetNeighbours(Grid.Tile tile, int area)
    {
        Vector2Int[] neighbourDirs = {
            new(area, 0),
            new(-area, 0),
            new(0, area),
            new(0, -area),
            new(area, area),
            new(-area, -area),
            new(-area, area),
            new(area, -area),
            
        };
        
        List<Grid.Tile> neighbours = new List<Grid.Tile>();
        foreach (var dir in neighbourDirs)
        {
            Grid.Tile temp = Grid.Instance.TryGetTile(new Vector2Int(tile.x + dir.x, tile.y + dir.y));

            if (temp != null)
            {
                if (closestZombie)
                    if (GetDistance(temp, closestZombie.GetCurrentTile) < ContextRadius)
                        continue;


                neighbours.Add(temp);
            }
        }
        
        return neighbours;
    }
    int GetDistance(Grid.Tile tile1, Grid.Tile tile2)
    {
        int distanceX = Mathf.Abs(tile1.x - tile2.x);
        int distanceY = Mathf.Abs(tile1.y - tile2.y);
        
        if (distanceX > distanceY)
            return 14 * distanceY + 10 * (distanceX - distanceY);
        else
            return 14 * distanceX + 10 * (distanceY - distanceX);
    }

    void FindPath(Vector2Int targetPos)
    {
        Grid.Tile startTile = myCurrentTile;
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
                RetracePath(startTile, targetTile);
                return;
            }

            foreach (Grid.Tile neighbour in GetNeighbours(currentTile, 1))
            {
                if (neighbour.occupied || closedSet.Contains(neighbour))
                    continue;

                int newCostNeighbour = currentTile.gCost + GetDistance(currentTile, neighbour);
                if (newCostNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newCostNeighbour;
                    neighbour.hCost = GetDistance(neighbour, targetTile);
                    neighbour.parent = currentTile;

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                        Grid.Instance.scannedTiles = closedSet;
                    }
                }
            }
        }
    }

    void RetracePath(Grid.Tile startTile, Grid.Tile targetTile)
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
    public override void UpdateCharacter()
    {
        base.UpdateCharacter();
        
        closestZombie = GetClosest(GetContextByTag("Zombie"))?.GetComponent<Zombie>();

        FindPath(new Vector2Int(Targets[TargetIndex].x, Targets[TargetIndex].y));
        SetWalkBuffer(Grid.Instance.path);
    }

    Vector3 GetEndPoint()
    {
        return Grid.Instance.WorldPos(Grid.Instance.GetFinishTile());
    }

    GameObject[] GetContextByTag(string aTag)
    {
        Collider[] context = Physics.OverlapSphere(transform.position, ContextRadius);
        List<GameObject> returnContext = new List<GameObject>();
        foreach (Collider c in context)
        {
            if (c.transform.CompareTag(aTag))
            {
                returnContext.Add(c.gameObject);
            }
        }
        return returnContext.ToArray();
    }

    GameObject GetClosest(GameObject[] aContext)
    {
        float dist = float.MaxValue;
        GameObject Closest = null;
        foreach (GameObject z in aContext)
        {
            float curDist = Vector3.Distance(transform.position, z.transform.position);
            if (curDist < dist)
            {
                dist = curDist;
                Closest = z;
            }
        }
        return Closest;
    }

    public int getTargetIndex() => TargetIndex;

    public void setTargetIndex(int index) => TargetIndex = index;

    public int getMaxIndex() => 2;
}
