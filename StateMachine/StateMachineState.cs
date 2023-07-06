using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StateMachineIds : int
{
    None                = 0,
    GroundedMovement    = 1,
    JumpingMovement     = 2,
    FallingMovement     = 3,
}

public class StateMachineState
{
    public StateMachineState(StateMachine stateMachine, StateMachineIds stateId)
    {
        _stateMachine = stateMachine;
        _stateId = stateId;
    }

    private StateMachine _stateMachine;
    private StateMachineIds _stateId;

    public StateMachine GetStateMachine() { return _stateMachine; }
    public StateMachineIds GetId() { return _stateId; }

    public virtual void OnEnter() { }
    public virtual void OnUpdate() { }
    public virtual void OnLeave() { }

}
