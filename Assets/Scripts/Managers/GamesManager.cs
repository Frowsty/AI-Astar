using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamesManager : StateMachine
{
    // _     __________    _    ______   __   ____ ___  ____  _____           
    //| |   | ____/ ___|  / \  / ___\ \ / /  / ___/ _ \|  _ \| ____|          
    //| |   |  _|| |  _  / _ \| |    \ V /  | |  | | | | | | |  _|            
    //| |___| |__| |_| |/ ___ | |___  | |   | |__| |_| | |_| | |___           
    //|_____|_____\____/_/   \_\____| |_|    \____\___/|____/|_____|          


    // ____   ___    _   _  ___ _____    ____ _   _    _    _   _  ____ _____ 
    //|  _ \ / _ \  | \ | |/ _ |_   _|  / ___| | | |  / \  | \ | |/ ___| ____|
    //| | | | | | | |  \| | | | || |   | |   | |_| | / _ \ |  \| | |  _|  _|  
    //| |_| | |_| | | |\  | |_| || |   | |___|  _  |/ ___ \| |\  | |_| | |___ 
    //|____/ \___/  |_| \_|\___/ |_|    \____|_| |_/_/   \_|_| \_|\____|_____|

    #region Singleton
    public static GamesManager Instance;

    #endregion

    public GameObject myKim;

    float TotalGameTime;

    int BurgerCount;
    int CollectedBurgers;

    [SerializeField] bool SkipIntro = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InitializeStateMachine();
        BurgerCount = FindObjectsOfType<Burger>().Length;
        if (!SkipIntro)
        {
            ChangeState<IntroState>();
        }
        else
        {
            ChangeState<PlayingState>();
        }
    }

    private void Update()
    {
        UpdateStateMachine();
    }

    public Vector3 GetKimPos()
    {
        return myKim.transform.position;
    }

    public void SetTotalGameTime(float aTime)
    {
        TotalGameTime = aTime;
    }
    public float GetTotlatGameTime()
    {
        return TotalGameTime;
    }

    public void CollectBurger()
    {
        CollectedBurgers++;
    }

    public int GetBurgerCount()
    {
        return BurgerCount;
    }
    public int GetCollectedBurgers()
    {
        return CollectedBurgers;
    }
}
