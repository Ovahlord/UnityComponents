/*
* This file is part of the UnityComponents repository authored by Ovahlord (https://github.com/Ovahlord/UnityComponents)
* All components in this repository are royalty free and can be used for commercial purposes. Enjoy.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitActions", menuName = "UnitAction/UnitActions")]
public class UnitActions : ScriptableObject
{
    [Header("Sprint")]
    [SerializeField] private bool _enableSprint = true;
    [SerializeField][Min(0f)] private float _sprintStaminaCost = 20f;
    [SerializeField][Min(0f)] private float _sprintExhaustionRecoveryDelay = 1f;
    [SerializeField][Min(0)] private int _sprintExhaustionRecoveryThreshold = 20;

    [Header("Dodge")]
    [SerializeField] private bool _enableDodge = true;
    [SerializeField][Min(0)] private int _dodgeStaminaCost = 20;
    [SerializeField][Min(0f)] private float _dodgeRecoveryDelay = 0.5f;

    // Sprint
    public bool IsSprintEnabled { get { return _enableSprint; } }
    public float SprintStaminaCost { get { return _sprintStaminaCost; } }
    public float SprintExhaustionRecoveryDelay { get { return _sprintExhaustionRecoveryDelay; } }
    public int SprintExhaustionRecoveryThreshold { get { return _sprintExhaustionRecoveryThreshold; } }

    public bool IsDodgeEnabled { get { return _enableDodge; } }
    public int DodgeStaminaCost { get { return _dodgeStaminaCost; } }
    public float DodgeRecoveryDelay { get { return _dodgeRecoveryDelay; } }
}
