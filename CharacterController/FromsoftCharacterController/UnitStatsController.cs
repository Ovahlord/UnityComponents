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
    private readonly float[] _recoveryRateOverride = new float[(int)RessourceIndex.Max];
    private readonly float[] _recoveryDelay = new float[(int)RessourceIndex.Max];

    public int CurrentHealth { get { return (int)_currentValue[(int)RessourceIndex.Health]; } }
    public int CurrentPower { get { return (int)_currentValue[(int)RessourceIndex.Power]; } }
    public int CurrentStamina { get { return (int)_currentValue[(int)RessourceIndex.Stamina]; } }

    public float CurrentHealthPct { get { return _currentValue[(int)RessourceIndex.Health] / _unitStats.MaxHealth; } }
    public float CurrentPowerPct { get { return _currentValue[(int)RessourceIndex.Power] / _unitStats.MaxPower; } }
    public float CurrentStaminaPct { get { return _currentValue[(int)RessourceIndex.Stamina] / _unitStats.MaxStamina; } }

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
        {
            _recoveryDelay[i] = 0f;
            _recoveryRateOverride[i] = 0f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Ressource recovery
        for (int i = 0; i < (int)RessourceIndex.Max; ++i)
        {
            // The regeneration is currently delayed so we have to wait for the delay to expire before we can continue
            if (_recoveryDelay[i] > 0f)
                _recoveryDelay[i] -= Time.deltaTime;

            // The regeneration is prevent either by being already fully recovered or we are prevented by certain circumstances)
            float recoveryRate = _recoveryRateOverride[i] != 0f ? _recoveryRateOverride[i] : _recoveryRate[i];
            float targetValue = recoveryRate > 0f ? _maxValue[i] : 0f;
            if (_currentValue[i] == targetValue || recoveryRate == 0f || _recoveryDelay[i] > 0f)
                continue;

            // And finally, recover the ressource
            _currentValue[i] = Mathf.Clamp(_currentValue[i] + recoveryRate * Time.deltaTime, 0f, _maxValue[i]);
        }
    }

    public void SetRecoveryRateOverride(RessourceIndex ressourceIndex, float rate) { _recoveryRateOverride[(int)ressourceIndex] = rate; }
    public void SetRecoveryDelay(RessourceIndex ressourceIndex, float delay) { _recoveryDelay[(int)ressourceIndex] = delay; }
    public void ModifyRessource(RessourceIndex ressourceIndex, int amount)
    {
        if (_currentValue[(int)ressourceIndex] >= amount)
            _currentValue[(int)ressourceIndex] -= amount;
        else
            _currentValue[(int)ressourceIndex] = 0f;
    }
}
