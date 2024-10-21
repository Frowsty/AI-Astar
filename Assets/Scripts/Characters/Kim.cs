using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;
using Random = UnityEngine.Random;

public class Kim : CharacterController
{
    [SerializeField] float ContextRadius = 4f;
    private Grid.Tile startTile;
    public Burger[] Burgers;
    private List<Grid.Tile> Targets = new List<Grid.Tile>();
    private List<Zombie> Zombies = new List<Zombie>();
    private Zombie closestZombie;
    private Zombie closestZombieToTarget;
    public bool shouldUpdatePath = true;
    public int TargetIndex = 0;
    private List<Grid.Tile> path = new List<Grid.Tile>();
    private Grid.Tile currentTarget;
    private bool skipChecks = true;

    public override void StartCharacter()
    {
        base.StartCharacter();

        ContextRadius = 4f;
        
        Targets.Add(Grid.Instance.GetClosest(Burgers[0].transform.position));
        Targets.Add(Grid.Instance.GetClosest(Burgers[1].transform.position));
        Targets.Add(Grid.Instance.GetFinishTile());
        
        currentTarget = Targets[TargetIndex];

        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Zombie"))
        {
            Zombie zombie = obj.GetComponent<Zombie>();
            
            if (zombie != null)
                Zombies.Add(zombie);
        }
        
        closestZombie = GetClosest(GetContextByTag("Zombie"))?.GetComponent<Zombie>();

        startTile = myCurrentTile;

        TargetIndex = Random.Range(0, 2);
    }
    
    public List<Grid.Tile> GetNeighbours(Grid.Tile tile)
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
        Grid.Instance.scannedTiles.Clear();
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

            foreach (Grid.Tile neighbour in GetNeighbours(currentTile))
            {
                if (neighbour.occupied || closedSet.Contains(neighbour))
                    continue;

                int newCostNeighbour = currentTile.gCost + GetDistance(currentTile, neighbour);
                if (newCostNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newCostNeighbour;
                    neighbour.hCost = GetDistance(neighbour, targetTile);
                    neighbour.parent = currentTile;
                    
                    if (closestZombie)
                        if (isWithinDistance(neighbour, closestZombie.GetCurrentTile, (int)ContextRadius))
                            neighbour.hCost = 1000000;

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                        Grid.Instance.scannedTiles = closedSet;
                    }
                }
            }
        }
    }

    bool isWithinDistance(Grid.Tile tile1, Grid.Tile tile2, int distance)
    {
        return Mathf.Abs(tile1.x - tile2.x) < distance &&
               Mathf.Abs(tile1.y - tile2.y) < distance;
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

    bool pathInaccessible()
    {
        if (!closestZombieToTarget)
            return false;
        if (isWithinDistance(currentTarget, closestZombieToTarget.GetCurrentTile, (int)ContextRadius))
            return true;
        return false;
    }
    public override void UpdateCharacter()
    {
        base.UpdateCharacter();

        closestZombie = GetClosest(GetContextByTag("Zombie"))?.GetComponent<Zombie>();
        closestZombieToTarget = GetClosestToObject(GetContextByTag("Zombie"), Grid.Instance.WorldPos(currentTarget))?.GetComponent<Zombie>();
    
        if (closestZombie && isWithinDistance(myCurrentTile, closestZombie.GetCurrentTile, (int)ContextRadius) || skipDistChecks())
        {
            FindPath(new Vector2Int(Targets[TargetIndex].x, Targets[TargetIndex].y));
            SetWalkBuffer(Grid.Instance.path);
        }

        if (pathInaccessible())
        {
            Grid.Instance.path.Clear();
            SetWalkBuffer(Grid.Instance.path);
            updateTargetIndex();
        }
    }

    bool skipDistChecks()
    {
        bool temp = skipChecks;
        skipChecks = false;

        return temp;
    }
    
    public bool setSkipDistChecks(bool status) => skipChecks = true;

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
    
    GameObject GetClosestToObject(GameObject[] aContext, Vector3 objectPosition)
    {
        float dist = float.MaxValue;
        GameObject Closest = null;
        foreach (GameObject z in aContext)
        {
            float curDist = Vector3.Distance(objectPosition, z.transform.position);
            if (curDist < dist)
            {
                dist = curDist;
                Closest = z;
            }
        }
        return Closest;
    }

    public void updateTargetIndex()
    {
        int temp = TargetIndex;
        if (TargetIndex == 1 && Burgers[0].gameObject.activeSelf)
            TargetIndex = 0;
        else if (TargetIndex == 0 && Burgers[1].gameObject.activeSelf)
            TargetIndex = 1;
        else if (!Burgers[1].gameObject.activeSelf && !Burgers[0].gameObject.activeSelf)
            TargetIndex = 2;
        currentTarget = Targets[TargetIndex];
    }

    public int getMaxIndex() => 2;
}
