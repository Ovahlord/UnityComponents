/*
* This file is part of the UnityComponents repository authored by Ovahlord (https://github.com/Ovahlord/UnityComponents)
* All components in this repository are royalty free and can be used for commercial purposes. Enjoy.
*/

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput), typeof(PlayerCameraController), typeof(UnitMovementController))]
public class PlayerCharacterController : MonoBehaviour
{
    private UnitMovementController _movementController = null;
    private PlayerCameraController _cameraController = null;
    private Vector2 _moveInputValue = Vector2.zero;
    private bool _isPerformingSprint = false;

    private void Awake()
    {
        _movementController = GetComponent<UnitMovementController>();
        _cameraController = GetComponent<PlayerCameraController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_isPerformingSprint)
            _movementController.MotionType = UnitMovementController.MovementType.Sprint;
        else if (_moveInputValue.magnitude < 0.5f)
            _movementController.MotionType = UnitMovementController.MovementType.Walk;
        else
            _movementController.MotionType = UnitMovementController.MovementType.Run;

        Vector3 direction = Quaternion.Euler(0f, _cameraController.CameraYAngle, 0f) * new Vector3(_moveInputValue.x, 0f, _moveInputValue.y);
        _movementController.MotionDirection = direction.normalized;
    }

    public void OnMove(InputValue value) { _moveInputValue = value.Get<Vector2>(); }
    public void OnLook(InputValue value) { _cameraController.TurnValue = value.Get<Vector2>(); }
    public void OnSprint(InputValue value) { _isPerformingSprint = value.isPressed; }
    public void OnSprintStop(InputValue value) { _isPerformingSprint = value.isPressed; }
}
