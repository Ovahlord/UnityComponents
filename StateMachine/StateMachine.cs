using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class StateMachine
{
    public StateMachine(object owner)
    {
        _owner = owner;
    }

    private object _owner;
    private Dictionary<StateMachineIds, StateMachineState> _states = new();
    private StateMachineState _activeState;

    public T GetOwner<T>() where T : class
    {
        Debug.Assert(_owner.GetType() == typeof(T), "StateMachine.GetOwner: mismatching object type.");
        return (T)_owner;
    }

    public T GetState<T>(StateMachineIds stateId) where T : class
    {
        if (!_states.ContainsKey(stateId))
            return null;

        Debug.Assert(_states[stateId].GetType() == typeof(T), "StateMachine.GetState: mismatching object type.");

        return _states[stateId] as T;
    }

    public void Initialize(List<StateMachineIds> states)
    {
        foreach (StateMachineIds stateId in states)
        {
            if (_states.ContainsKey(stateId))
            {
                Debug.Assert(false, $"StateMachine.Initialize: { stateId } already has a state in the machine's state dictionary.");
                continue;
            }

            AddState(CreateStateForId(stateId));
        }
    }

    public void Update()
    {
        _activeState?.OnUpdate();
    }

    private void AddState(StateMachineState state)
    {
        if (_states.ContainsKey(state.GetId()))
            return;

        _states[state.GetId()] = state;
    }

    public void SetActiveState(StateMachineIds stateId)
    {
        if (!_states.ContainsKey(stateId))
            return;

        SetActiveState(_states[stateId]);
    }

    private void SetActiveState(StateMachineState state)
    {
        _activeState?.OnLeave();
        _activeState = state;
        _activeState.OnEnter();
    }

    public StateMachineState GetActiveState() { return _activeState; }

    private StateMachineState CreateStateForId(StateMachineIds stateId)
    {
        switch (stateId)
        {
            case StateMachineIds.GroundedMovement:  return new GroundedMovementState(this, stateId);
            case StateMachineIds.JumpingMovement:   return new JumpingMovementState(this, stateId);
            case StateMachineIds.FallingMovement:   return new FallingMovementState(this, stateId);
            default:
                Debug.Assert(false, "StateMachineState.CreateStateForId attempted to create a state machine state for an unsupported type.");
                return null;
        }
    }
}
