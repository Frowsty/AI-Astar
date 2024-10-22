using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.XR;
using Random = UnityEngine.Random;

public class Kim : CharacterController
{
    [SerializeField] float ContextRadius = 6f;
    private Grid.Tile startTile;
    private List<Zombie> Zombies = new List<Zombie>();
    private List<Burger> Burgers = new List<Burger>();
    private List<Grid.Tile> path = new List<Grid.Tile>();
    public bool foundPath = false;
    private Grid.Tile targetTile;
    private Grid.Tile finishTile;
    private float retryTimer = 0f;

    private Pathfinding pathFinder = new Pathfinding();
    private StateManager stateManager = new StateManager(); 

    public override void StartCharacter()
    {
        pathFinder.setPlayer(this);
        base.StartCharacter();

        ContextRadius = 6f;

        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Zombie"))
        {
            Zombie zombie = obj.GetComponent<Zombie>();
            
            if (zombie != null)
                Zombies.Add(zombie);
        }
        
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Burger"))
        {
            Burger burger = obj.GetComponent<Burger>();
            
            if (burger != null)
                Burgers.Add(burger);
        }
        startTile = myCurrentTile;
        finishTile = Grid.Instance.GetFinishTile();
    }

    int getActiveBurgerCount()
    {
        int activeCount = 0;
        foreach (Burger burger in Burgers)
        {
            if (burger.gameObject.activeSelf)
                activeCount++;
        }
        return activeCount;
    }

    void CheckContext()
    {
        List<GameObject> contexts = GetContexts();

        Tuple<StateManager.States, int> priorityState = new Tuple<StateManager.States, int>(StateManager.States.SEARCHING, stateManager.getStatePriority(StateManager.States.SEARCHING));
        foreach (GameObject obj in contexts)
        {
            if (obj.CompareTag("Zombie") && priorityState.Item2 > stateManager.getStatePriority(StateManager.States.EVADE))
                priorityState = new Tuple<StateManager.States, int>(StateManager.States.EVADE, stateManager.getStatePriority(StateManager.States.EVADE));
            if (obj.CompareTag("Burger") && priorityState.Item2 > stateManager.getStatePriority(StateManager.States.TARGET))
                priorityState = new Tuple<StateManager.States, int>(StateManager.States.TARGET, stateManager.getStatePriority(StateManager.States.TARGET));
        }

        stateManager.changeState(priorityState.Item1);
        
        resetTileCost();
    }

    void resetTileCost()
    {
        foreach (Grid.Tile tile in Grid.Instance.GetTiles())
        {
            tile.gCost = 0;
            tile.hCost = 0;
        }
    }

    void SetZombieRegion()
    {
        Zombie zombie = GetClosest(GetContextByTag("Zombie"))?.GetComponent<Zombie>();
        if (!zombie || !pathFinder.isWithinDistance(myCurrentTile, zombie.GetCurrentTile, 6))
            return;
        
        for (int x = zombie.GetCurrentTile.x - 3; x <= zombie.GetCurrentTile.x + 3; x++)
        {
            for (int y = zombie.GetCurrentTile.y - 3; y <= zombie.GetCurrentTile.y + 3; y++)
            {
                Grid.Tile temp = Grid.Instance.TryGetTile(new Vector2Int(x, y));
                if (temp != null)
                    temp.hCost = 1000000;
            }
        }
    }

    Grid.Tile closestBurger()
    {
        Burger burger = GetClosest(GetContextByTag("Burger"))?.GetComponent<Burger>();
        if (!burger)
            return myCurrentTile;
        
        Grid.Tile returnTile = Grid.Instance.GetClosest(burger.transform.position);

        if (returnTile == null)
            return myCurrentTile;
        return returnTile;
    }
    
    bool avoidFinish()
    {
        return getActiveBurgerCount() > 0 && pathFinder.isWithinDistance(myCurrentTile, finishTile, 4);
    }

    bool isPathWalkable()
    {
        foreach (Grid.Tile tile in Grid.Instance.path)
        {
            if (tile.hCost == 1000000)
                return false;
        }

        return true;
    }
    
    public override void UpdateCharacter()
    {
        base.UpdateCharacter();
        
        CheckContext();

        switch (stateManager.getCurrentState())
        {
            case StateManager.States.EVADE:
                SetZombieRegion();
                goto case StateManager.States.SEARCHING;
            case StateManager.States.SEARCHING:
                if (!foundPath || myCurrentTile.isEqual(targetTile) || avoidFinish() || !isPathWalkable())
                {
                    do
                    {
                        targetTile = Grid.Instance.GetTiles()[Random.Range(0, Grid.Instance.tiles.Count)];
                    } while (targetTile.occupied);
                }

                if (getActiveBurgerCount() == 0)
                    targetTile = finishTile;
                break;
            case StateManager.States.TARGET:
                SetZombieRegion();
                if (!isPathWalkable())
                    goto case StateManager.States.SEARCHING;
                if (retryTimer >= 3f)
                {
                    retryTimer = 0f;
                    targetTile = closestBurger();
                }

                break;
        }

        if (targetTile != null)
        {
            foundPath = pathFinder.FindPath(new Vector2Int(targetTile.x, targetTile.y));
            SetWalkBuffer(Grid.Instance.path);
        }

        retryTimer += Time.fixedDeltaTime;
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

    List<GameObject> GetContexts()
    {
        Collider[] context = Physics.OverlapSphere(transform.position, ContextRadius);
        List<GameObject> returnContext = new List<GameObject>();
        foreach (Collider c in context)
        {
            returnContext.Add(c.gameObject);
        }
        return returnContext;
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
    
    public Grid.Tile getCurrentTile() => myCurrentTile;
}
