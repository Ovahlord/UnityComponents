using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StateMachineStateFactory
{
    public static StateMachineState CreateStateForId(StateMachine stateMachine, StateMachineIds id)
    {
        switch (id)
        {
            case StateMachineIds.GroundedMovement:      return new StateMachineState(stateMachine, id);
            case StateMachineIds.JumpingMovement:       return new StateMachineState(stateMachine, id);
            case StateMachineIds.FallingMovement:       return new StateMachineState(stateMachine, id);
            case StateMachineIds.SlopeSlidingMovement:  return new StateMachineState(stateMachine, id);
            default:
                Debug.Assert(false, $"Attempted to create a state machine state for unsupported state { id }");
                return null;
        }
    }
}
