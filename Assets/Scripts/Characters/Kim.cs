using System.Collections;
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
    public bool shouldUpdatePath = true;
    public int TargetIndex = 0;
    private List<Grid.Tile> path = new List<Grid.Tile>();
    private List<Zombie> closestZombies = new List<Zombie>();

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

        startTile = myCurrentTile;
    }
    
    public List<Grid.Tile> GetNeighbours(Grid.Tile tile)
    {
        Vector2Int[] neighbourDirs = {
            new(1, 0),
            new(-1, 0),
            new(0, 1),
            new(0, -1)
        };
        
        List<Grid.Tile> neighbours = new List<Grid.Tile>();
        foreach (var dir in neighbourDirs)
        {
            var temp = Grid.Instance.TryGetTile(new Vector2Int(tile.x + dir.x, tile.y + dir.y));
            
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
    
    public List<Grid.Tile> GetBounds(Grid.Tile node, int range) {
        List<Grid.Tile> neighbours = new List<Grid.Tile>();
        
        foreach (Grid.Tile tile in Grid.Instance.tiles)
        {
            if ((Mathf.Abs(node.x - tile.x) == range || Mathf.Abs(node.y - tile.y) == range)
                || (Mathf.Abs(node.x - tile.x) < range || Mathf.Abs(node.y - tile.y) < range))
                neighbours.Add(tile);
        }

        return neighbours;
    }

    void FindPath(Vector2Int startPos, Vector2Int targetPos)
    {
        Grid.Tile startTile = Grid.Instance.TryGetTile(startPos);
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
            
            if (closestZombies.Count() > 0)
            {
                bool tooClose = false;
                foreach (Zombie zombie in closestZombies)
                {
                    if (GetDistance(currentTile, Grid.Instance.GetClosest(zombie.transform.position)) < ContextRadius)
                    {
                        tooClose = true;
                    }
                }

                if (tooClose)
                    continue;
            }
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

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                        Grid.Instance.scannedTiles = closedSet;
                    }
                }
            }
        }

        closestZombies.Clear();
    }

    bool CheckZombiesAgainstPath()
    {
        foreach (Grid.Tile tile in Grid.Instance.path)
        {
            foreach (Zombie zombie in Zombies)
            {
                if (!zombie)
                    continue;
                
                if (GetDistance(tile, Grid.Instance.GetClosest(zombie.transform.position)) < ContextRadius)
                {
                    closestZombies.Add(zombie);
                    return true;
                }
            }
        }
        return false;
    }

    void RetracePath(Grid.Tile startTile, Grid.Tile targetTile)
    {
        List<Grid.Tile> path = new List<Grid.Tile>();
        Grid.Tile currentNode = targetTile;

        while (currentNode != startTile)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        
        path.Reverse();

        Grid.Instance.path = path;
    }
    
    public Thread StartPathfinding(Vector2Int param1, Vector2Int param2) {
        var t = new Thread(() => FindPath(param1, param2));
        t.Start();
        return t;
    }

    public override void UpdateCharacter()
    {
        base.UpdateCharacter();
        
        //Zombie closest = GetClosest(GetContextByTag("Zombie"))?.GetComponent<Zombie>();
        
        if (shouldUpdatePath)
        {
            StartPathfinding(new Vector2Int(myCurrentTile.x, myCurrentTile.y),
                    new Vector2Int(Targets[TargetIndex].x, Targets[TargetIndex].y));
        }

        if (shouldUpdatePath)
        {
            shouldUpdatePath = false;
            SetWalkBuffer(Grid.Instance.path);
        }
        
        shouldUpdatePath = CheckZombiesAgainstPath();
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

    public void setTargetIndex(int index)
    {
        shouldUpdatePath = true;
        TargetIndex = index;
    }

    public int getMaxIndex() => 2;
}
