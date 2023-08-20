using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachineState
{
    public StateMachineState(StateMachine stateMachine, StateMachineIds stateId)
    {
        StateMachine = stateMachine;
        Id = stateId;
    }

    public StateMachine StateMachine { get; private set; }
    public StateMachineIds Id { get; private set; }

    public virtual void OnEnter() { }
    public virtual void OnUpdate() { }
    public virtual void OnLeave() { }
}
