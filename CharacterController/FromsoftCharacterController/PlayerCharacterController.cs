/*
* This file is part of the UnityComponents repository authored by Ovahlord (https://github.com/Ovahlord/UnityComponents)
* All components in this repository are royalty free and can be used for commercial purposes. Enjoy.
*/

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput), typeof(PlayerCameraController))]
[RequireComponent(typeof(UnitActionController))]
public class PlayerCharacterController : MonoBehaviour
{
    [SerializeField] private GameObject _playerHudPrefab = null;

    private UnitActionController _actionController = null;
    private PlayerCameraController _cameraController = null;
    private Vector2 _moveInputValue = Vector2.zero;

    private void Awake()
    {
        _actionController = GetComponent<UnitActionController>();
        _cameraController = GetComponent<PlayerCameraController>();
    }

    private void Start()
    {
        Cursor.visible = false;
        if (_playerHudPrefab != null)
            Instantiate(_playerHudPrefab, Vector3.zero, Quaternion.identity, null);
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 direction = Quaternion.Euler(0f, _cameraController.CameraYAngle, 0f) * new Vector3(_moveInputValue.x, 0f, _moveInputValue.y);
        _actionController.MoveDirection(direction);
    }

    public void OnMove(InputValue value)
    {
        _moveInputValue = value.Get<Vector2>();
        if (_moveInputValue.magnitude <= 0.5f)
            _actionController.SetMotionType(UnitMovementController.MovementType.Walk);
        else
            _actionController.SetMotionType(UnitMovementController.MovementType.Run);
    }

    public void OnLook(InputValue value) { _cameraController.TurnValue = value.Get<Vector2>(); }
    public void OnSprint(InputValue value) { _actionController.AutoSprint = true; }
    public void OnSprintStop(InputValue value) { _actionController.AutoSprint = false; }
    public void OnDodge(InputValue value) { _actionController.PerformDodge(); }
}
