using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateManager
{
    public enum States
    {
        TARGET = 0,
        EVADE,
        SEARCHING
    }

    States currentState = States.SEARCHING;

    public void changeState(States newState) => currentState = newState;
    
    public States getCurrentState() => currentState;
    
    public int getStatePriority(States state) => (int)state;
}
