/*
* This file is part of the UnityComponents repository authored by Ovahlord (https://github.com/Ovahlord/UnityComponents)
* All components in this repository are royalty free and can be used for commercial purposes. Enjoy.
*/

using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(PlayerInput), typeof(PlayerCameraController))]
[RequireComponent(typeof(UnitActionController))]
public class PlayerCharacterController : MonoBehaviour
{
    [Header("HUD Settings")]
    [SerializeField] private GameObject _playerHudPrefab = null;

    private UnitActionController _actionController = null;
    private PlayerInput _input = null;
    private InputAction _dodgeAction = null;
    private Vector2 _moveInputValue = Vector2.zero;

    private void Awake()
    {
        _actionController = GetComponent<UnitActionController>();
        _input = GetComponent<PlayerInput>();
        _dodgeAction = _input.actions["Dodge"];
    }

    private void Start()
    {
        Cursor.visible = false;
        if (_playerHudPrefab != null)
            Instantiate(_playerHudPrefab, Vector3.zero, Quaternion.identity, null);
    }

    private void Update()
    {
        // Update the motion information based on the current movement input values and camera direction
        UpdateMotionData();
    }

    public void OnMove(InputValue value)
    {
        _moveInputValue = value.Get<Vector2>();
    }

    public void OnLook(InputValue value) { PlayerCameraController.TurnInputValue = value.Get<Vector2>(); }

    public void OnSprint(InputValue value)
    {
        _actionController.AutoSprint = true;
        _dodgeAction?.Disable();
    }

    public void OnSprintStop(InputValue value)
    {
        _actionController.AutoSprint = false;
        _dodgeAction?.Enable();
    }

    public void OnDodge(InputValue value) { _actionController.PerformDodge(); }

    public void OnLockCamera(InputValue value) { PlayerCameraController.ToggleCameraLock(); }

    private void UpdateMotionData()
    {
        Vector3 direction = Quaternion.Euler(0f, PlayerCameraController.CameraYAngle, 0f) * new Vector3(_moveInputValue.x, 0f, _moveInputValue.y);

        UnitMovementController.MovementType motionType = UnitMovementController.MovementType.Run;
        if (_actionController.HasSprintEnabled)
            motionType = UnitMovementController.MovementType.Sprint;
        else if (_moveInputValue.magnitude <= 0.5f)
            motionType = UnitMovementController.MovementType.Walk;

        _actionController.SetMotionData(direction.normalized, motionType);
    }
}
