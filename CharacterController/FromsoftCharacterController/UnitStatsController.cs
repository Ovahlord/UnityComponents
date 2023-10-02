/*
* This file is part of the UnityComponents repository authored by Ovahlord (https://github.com/Ovahlord/UnityComponents)
* All components in this repository are royalty free and can be used for commercial purposes. Enjoy.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitStatsController : MonoBehaviour
{
    [SerializeField] private UnitStats _unitStats;

    private readonly float[] _maxValue = new float[(int)RessourceIndex.Max];
    private readonly float[] _currentValue = new float[(int)RessourceIndex.Max];
    private readonly float[] _recoveryRate = new float[(int)RessourceIndex.Max];
    private readonly float[] _recoveryDelay = new float[(int)RessourceIndex.Max];

    private float _sprintStaminaCost = 0f;
    private float _exhaustionRecoveryDelay = 0f;
    private int _dodgeStaminaCost = 0;
    private float _dodgeRecoveryDelay = 0f;

    private bool _isRecoveringFromExhaustion = false;

    public int CurrentHealth { get { return (int)_currentValue[(int)RessourceIndex.Health]; } }
    public int CurrentPower { get { return (int)_currentValue[(int)RessourceIndex.Power]; } }
    public int CurrentStamina { get { return (int)_currentValue[(int)RessourceIndex.Stamina]; } }

    public float CurrentHealthPct { get { return _currentValue[(int)RessourceIndex.Health] / _unitStats.MaxHealth; } }
    public float CurrentPowerPct { get { return _currentValue[(int)RessourceIndex.Power] / _unitStats.MaxPower; } }
    public float CurrentStaminaPct { get { return _currentValue[(int)RessourceIndex.Stamina] / _unitStats.MaxStamina; } }

    public bool IsSprinting { get; private set; }
    public bool CanPerformDodge { get { return _currentValue[(int)RessourceIndex.Stamina] >= _dodgeStaminaCost; } }

    private void Awake()
    {
        _maxValue[(int)RessourceIndex.Health] = _unitStats.MaxHealth;
        _maxValue[(int)RessourceIndex.Power] = _unitStats.MaxPower;
        _maxValue[(int)RessourceIndex.Stamina] = _unitStats.MaxStamina;

        _currentValue[(int)RessourceIndex.Health] = _unitStats.StartHealth;
        _currentValue[(int)RessourceIndex.Power] = _unitStats.StartPower;
        _currentValue[(int)RessourceIndex.Stamina] = _unitStats.StartStamina;

        _recoveryRate[(int)RessourceIndex.Health] = _unitStats.HealthRecoveryRate;
        _recoveryRate[(int)RessourceIndex.Power] = _unitStats.PowerRecoveryRate;
        _recoveryRate[(int)RessourceIndex.Stamina] = _unitStats.StaminaRecoveryRate;

        for (int i = 0; i < (int)RessourceIndex.Max; ++i)
            _recoveryDelay[i] = 0f;

        _sprintStaminaCost = _unitStats.SprintStaminaCost;
        _exhaustionRecoveryDelay = _unitStats.ExhaustionRecoveryDelay;
        _dodgeStaminaCost = _unitStats.DodgeStaminaCost;
        _dodgeRecoveryDelay = _unitStats.DodgeRecoveryDelay;
    }

    // Update is called once per frame
    void Update()
    {
        // Sprinting
        if (IsSprinting)
        {
            // We are currently sprinting. Drain stamina and disable sprinting if we are running out of it.
            _currentValue[(int)RessourceIndex.Stamina] -= _sprintStaminaCost * Time.deltaTime;
            if (_currentValue[(int)RessourceIndex.Stamina] <= 0f)
            {
                _currentValue[(int)RessourceIndex.Stamina] = 0f;
                IsSprinting = false;

                // If we hit zero stamina while sprinting, we trigger a recovery phase which lasts until we have recovered 20% stamina
                _isRecoveringFromExhaustion = true;
                _recoveryDelay[(int)RessourceIndex.Stamina] = _exhaustionRecoveryDelay;
            }
            return;
        }

        // Ressource regeneration
        for (int i = 0; i < (int)RessourceIndex.Max; ++i)
        {
            // The regeneration is currently delayed so we have to wait for the delay to expire before we can continue
            if (_recoveryDelay[i] > 0f)
                _recoveryDelay[i] -= Time.deltaTime;

            if (_currentValue[i] == _maxValue[i] || _recoveryRate[i] == 0f || _recoveryDelay[i] > 0f)
                continue;

            _currentValue[i] = Mathf.Min(_currentValue[i] + _recoveryRate[i] * Time.deltaTime, _maxValue[i]);

            if (i == (int)RessourceIndex.Stamina)
                if (CurrentStaminaPct >= 0.2f)
                    _isRecoveringFromExhaustion = false;
        }
    }

    public void ToggleSprint(bool enable)
    {
        if (IsSprinting == enable || _isRecoveringFromExhaustion)
            return;

        IsSprinting = enable;
    }
    public void DrainDodgeStamina()
    {
        _currentValue[(int)RessourceIndex.Stamina] -= _dodgeStaminaCost;
        _recoveryDelay[(int)RessourceIndex.Stamina] = _dodgeRecoveryDelay;
    }
}
