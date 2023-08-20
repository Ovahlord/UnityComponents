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
    public StateMachineState ActiveState { get; private set; } = null;
    public StateMachineState PreviousActiveState { get; private set; } = null;

    public T GetOwner<T>() where T : class
    {
        Debug.Assert(_owner.GetType() == typeof(T), "StateMachine.GetOwner: mismatching object type.");
        return _owner as T;
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

            AddState(StateMachineStateFactory.CreateStateForId(this, stateId));
        }
    }

    public void Update()
    {
        ActiveState?.OnUpdate();
    }

    private void AddState(StateMachineState state)
    {
        if (_states.ContainsKey(state.Id))
            return;

        _states[state.Id] = state;
    }

    public void SetActiveState(StateMachineIds stateId)
    {
        if (!_states.ContainsKey(stateId))
            return;

        SetActiveState(_states[stateId]);
    }

    private void SetActiveState(StateMachineState state)
    {
        ActiveState?.OnLeave();
        PreviousActiveState = ActiveState;
        ActiveState = state;
        ActiveState.OnEnter();
    }
}
