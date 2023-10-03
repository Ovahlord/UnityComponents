/*
* This file is part of the UnityComponents repository authored by Ovahlord (https://github.com/Ovahlord/UnityComponents)
* All components in this repository are royalty free and can be used for commercial purposes. Enjoy.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RessourceIndex : int
{
    Health  = 0,
    Power   = 1,
    Stamina = 2,
    Max
}

[CreateAssetMenu( fileName = "UnitStats", menuName = "UnitStats/UnitStats")]
public class UnitStats : ScriptableObject
{
    [Header("Health")]
    [SerializeField][Min(1)] private int _maxHealth = 100;
    [SerializeField][Min(0)] private int _startHealth = 100;
    [SerializeField][Min(0)] private float _healthRecoveryRate = 0f;

    [Header("Power")]
    [SerializeField][Min(1)] private int _maxPower = 100;
    [SerializeField][Min(0)] private int _startPower = 100;
    [SerializeField][Min(0)] private float _powerRecoveryRate = 0f;

    [Header("Stamina")]
    [SerializeField][Min(1)] private int _maxStamina = 100;
    [SerializeField][Min(0)] private int _startStamina = 100;
    [SerializeField][Min(0f)] private float _staminaRecoveryRate = 20f;

    public int MaxHealth { get { return _maxHealth; } }
    public int StartHealth { get { return _startHealth; } }
    public float HealthRecoveryRate { get { return _healthRecoveryRate; } }

    public int MaxPower { get { return _maxPower; } }
    public int StartPower { get { return _startPower; } }
    public float PowerRecoveryRate { get { return _powerRecoveryRate; } }

    public int MaxStamina { get { return _maxStamina; } }
    public int StartStamina { get { return _startStamina; } }
    public float StaminaRecoveryRate { get { return _staminaRecoveryRate; } }
}
