/*
* This file is part of the UnityComponents repository authored by Ovahlord (https://github.com/Ovahlord/UnityComponents)
* All components in this repository are royalty free and can be used for commercial purposes. Enjoy.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitMovementController), typeof(UnitStatsController))]
public class UnitActionController : MonoBehaviour
{
    [SerializeField] private UnitActions _actions = null;

    private UnitMovementController _movementController = null;
    private UnitStatsController _statsController = null;
    private int _sprintStaminaRecoveryThreshold = 0;
    private bool _autoSprint = false;

    public bool HasSprintEnabled { get; private set; }
    public bool AutoSprint
    {
        get { return _autoSprint; }
        set
        {
            // AutoSprint has been disabled, cancel the current sprint right away
            _autoSprint = value;
            if (!_autoSprint)
                ToggleSprint(false);
        }
    }

    public bool CanSprint 
    { 
        get 
        {
            // The unit has not enough stamina
            if (_statsController.CurrentStamina == 0)
                return false;

            // The unit is not moving right now
            if (_movementController.MotionDirection.magnitude == 0f)
                return false;

            // The unit has yet to reach a certain stamina threshold before being allowed to sprint again
            if (_statsController.CurrentStamina < _sprintStaminaRecoveryThreshold)
                return false;

            return true;
        }
    }

    private void Awake()
    {
        _movementController = GetComponent<UnitMovementController>();
        _statsController = GetComponent<UnitStatsController>();
    }

    // Update is called once per frame
    void Update()
    {
        // Clear the stamina recovery threshold for sprinting so we can start sprinting again
        if (_sprintStaminaRecoveryThreshold > 0 && _statsController.CurrentStamina >= _sprintStaminaRecoveryThreshold)
            _sprintStaminaRecoveryThreshold = 0;

        if (HasSprintEnabled && !CanSprint)
            ToggleSprint(false);
        else if (AutoSprint && CanSprint)
            ToggleSprint(true);
    }

    public void SetMotionData(Vector3 direction, UnitMovementController.MovementType movementType)
    {
        _movementController.MotionDirection = direction;
        _movementController.MotionType = movementType;
    }

    // Enables the sprinting mechanic and will toggle off automatically when the unit has run out of stamina
    public bool ToggleSprint(bool enable)
    {
        if (_actions == null || !_actions.IsSprintEnabled || HasSprintEnabled == enable || (enable && !CanSprint))
            return false;

        HasSprintEnabled = enable;

        if (enable)
            _statsController.SetRecoveryRateOverride(RessourceIndex.Stamina, -_actions.SprintStaminaCost);
        else
        {
            _statsController.SetRecoveryRateOverride(RessourceIndex.Stamina, 0f);

            // If we ran out of stamina, apply a recovery delay and set a expected recovery threshold before we can sprint again
            if (_statsController.CurrentStamina == 0f)
            {
                _statsController.SetRecoveryDelay(RessourceIndex.Stamina, _actions.SprintExhaustionRecoveryDelay);
                _sprintStaminaRecoveryThreshold = _actions.SprintExhaustionRecoveryThreshold;
            }

        }

        return true;
    }

    public bool PerformDodge()
    {
        if (_actions == null || !_actions.IsDodgeEnabled)
            return false;

        if (_statsController.CurrentStamina == 0)
            return false;

        _statsController.ModifyRessource(RessourceIndex.Stamina, _actions.DodgeStaminaCost);
        _statsController.SetRecoveryDelay(RessourceIndex.Stamina, _actions.DodgeRecoveryDelay);

        return true;
    }
}
