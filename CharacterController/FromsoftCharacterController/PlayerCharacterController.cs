/*
* This file is part of the UnityComponents repository authored by Ovahlord (https://github.com/Ovahlord/UnityComponents)
* All components in this repository are royalty free and can be used for commercial purposes. Enjoy.
*/

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput), typeof(PlayerCameraController), typeof(UnitMovementController))]
[RequireComponent(typeof(UnitStatsController))]
public class PlayerCharacterController : MonoBehaviour
{
    [SerializeField] private GameObject _playerHudPrefab = null;

    private UnitMovementController _movementController = null;
    private UnitStatsController _unitStatController = null;
    private PlayerCameraController _cameraController = null;
    private PlayerHUDController _hudController = null;
    private Vector2 _moveInputValue = Vector2.zero;

    private void Awake()
    {
        _movementController = GetComponent<UnitMovementController>();
        _cameraController = GetComponent<PlayerCameraController>();
        _unitStatController = GetComponent<UnitStatsController>();
    }

    private void Start()
    {
        if (_playerHudPrefab != null)
        {
            GameObject hud = Instantiate(_playerHudPrefab, Vector3.zero, Quaternion.identity, null);
            _hudController = hud.GetComponent<PlayerHUDController>();
            _hudController.UpdatePlayerBars(_unitStatController.CurrentHealthPct, _unitStatController.CurrentPowerPct, _unitStatController.CurrentStaminaPct);
        }

        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (_unitStatController.IsSprinting)
            _movementController.MotionType = UnitMovementController.MovementType.Sprint;
        else if (_moveInputValue.magnitude < 0.5f)
            _movementController.MotionType = UnitMovementController.MovementType.Walk;
        else
            _movementController.MotionType = UnitMovementController.MovementType.Run;

        Vector3 direction = Quaternion.Euler(0f, _cameraController.CameraYAngle, 0f) * new Vector3(_moveInputValue.x, 0f, _moveInputValue.y);
        _movementController.MotionDirection = direction.normalized;

        _hudController.UpdatePlayerBars(_unitStatController.CurrentHealthPct, _unitStatController.CurrentPowerPct, _unitStatController.CurrentStaminaPct);
    }

    public void OnMove(InputValue value) { _moveInputValue = value.Get<Vector2>(); }
    public void OnLook(InputValue value) { _cameraController.TurnValue = value.Get<Vector2>(); }
    public void OnSprint(InputValue value) { _unitStatController.ToggleSprint(value.isPressed); }
    public void OnSprintStop(InputValue value) { _unitStatController.ToggleSprint(value.isPressed); }
    public void OnDodge(InputValue value)
    {
        if (!value.isPressed && _unitStatController.CanPerformDodge)
            _unitStatController.DrainDodgeStamina();
    }
}
