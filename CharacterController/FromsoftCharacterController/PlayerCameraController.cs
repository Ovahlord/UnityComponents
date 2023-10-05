/*
* This file is part of the UnityComponents repository authored by Ovahlord (https://github.com/Ovahlord/UnityComponents)
* All components in this repository are royalty free and can be used for commercial purposes. Enjoy.
*/

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using static UnityEngine.UI.Image;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Camera Following Settings")]
    [SerializeField][Range(0f, 1f)] private float _facingPointDampening = 0.2f;
    [SerializeField][Range(0f, 1f)] private float _cameraMovementDampening = 0.8f;
    [SerializeField][Min(0f)] private float _heightOffset = 2f;
    [SerializeField][Min(0f)] private float _cameraDistance = 10f;
    [SerializeField] private LayerMask _collisionLayerMask = 0;


    [Header("Camera Rotation Settings")]
    [SerializeField][Range(-90f, 90f)] private float _initialCameraLearnAngle = 20f;
    [SerializeField][Range(-89f, 0f)] private float _minCameraLeanAngle = -30;
    [SerializeField][Range(0f, 90f)] private float _maxCameraLeanAngle = 60;
    [SerializeField][Range(-90f, 90f)] private float _lockedCameraLeanAngle = 20f;
    public Vector2 TurnInputValue { get; set; }
    public float CameraYAngle { get { return _camera.eulerAngles.y; } }
    public bool IsLockedToTarget { get { return _lockTarget != null; } }

    private bool _isUsingGamepad = false;
    private float? _lockFacingTimer = null;
    private Transform _camera = null;
    private Transform _lockTarget = null;
    private Vector3 _originPoint = Vector3.zero;
    private Vector3 _facingPoint = Vector3.zero;
    private Vector3 _targetRotation = Vector3.zero;

    private void Awake()
    {
        InputUser.onChange += (InputUser user, InputUserChange change, InputDevice device) =>
        {
            if (change == InputUserChange.ControlSchemeChanged)
                _isUsingGamepad = user.controlScheme.Value.name.Equals("Gamepad");
        };
    }

    private void Start()
    {
        _camera = Camera.main.transform;
        _originPoint = transform.position + Vector3.up * _heightOffset;
        _facingPoint = _originPoint;
        Quaternion startRotation = Quaternion.Euler(_initialCameraLearnAngle, transform.eulerAngles.y, transform.eulerAngles.z);
        _camera.SetPositionAndRotation(_originPoint + startRotation * Vector3.back * _cameraDistance, startRotation);
        _targetRotation = startRotation.eulerAngles;
    }

    private void LateUpdate()
    {
        _originPoint = transform.position + Vector3.up * _heightOffset;

        // Update the position of our lerping point
        if (_facingPointDampening == 0f)
            _facingPoint = _originPoint;
        else
            _facingPoint = Vector3.Lerp(_facingPoint, _originPoint, (1f - _facingPointDampening) * Time.deltaTime * 10f);

        MoveCamera();
    }

    // Returns a vector which calculates the effective offsets for turning input
    private Vector2 GetInputOffsets()
    {
        if (TurnInputValue.magnitude == 0f)
            return Vector2.zero;

        Vector2 offset = TurnInputValue;

        // Gamepad input values are in degrees rather than delta so we have to scale it with deltaTime
        if (_isUsingGamepad)
            offset *= Time.deltaTime;

        return offset;
    }

    private void MoveCamera()
    {
        // The point that we will calculate our angle delta against to make the mover orbit arround our camera
        Vector3 oldRotation = _camera.eulerAngles;
        Vector3 deltaAnglePoint = _lockTarget != null ? _lockTarget.position : _facingPoint;

        // Apply the difference between our current and our target rotation. Ignore the X axis because that one is fixed and can only be changed by input.
        _camera.LookAt(deltaAnglePoint);
        _targetRotation.y = _camera.eulerAngles.y;
        _targetRotation.z = _camera.eulerAngles.z;

        // We have ongoing camera movement input. Input is going to rotate the camera arround the target instantly without any lerp
        if (TurnInputValue.magnitude != 0f && _lockTarget == null)
        {
            Vector2 turnOffset = GetInputOffsets();

            // Apply the X input to our target roation. The remaining y and z will get updated outside of the input handling
            _targetRotation.x = Mathf.Clamp(Mathf.DeltaAngle(0f, _targetRotation.x) - turnOffset.y, _minCameraLeanAngle, _maxCameraLeanAngle);

            // Now we are going to orbit the camera arround our facing point without any lerp
            Vector3 angle = _camera.eulerAngles;
            angle.y += turnOffset.x;
            angle.x = Mathf.DeltaAngle(0f, angle.x); // clamp the angle to a 180 degrees range (-180 to 180) so our angle limits can work

            // Only apply the camera X input if we are not beyond the lean angle limits already due to lerping
            if (angle.x > _minCameraLeanAngle && angle.x < _maxCameraLeanAngle)
                angle.x = Mathf.Clamp(Mathf.DeltaAngle(0f, angle.x) - turnOffset.y, _minCameraLeanAngle, _maxCameraLeanAngle);

            _camera.position = _facingPoint + Quaternion.Euler(angle) * (Vector3.back * Vector3.Distance(_camera.position, _facingPoint));
            _camera.LookAt(_facingPoint);
        }
        else if (_lockTarget != null)
            _targetRotation.x = _lockedCameraLeanAngle;

        // Estimate our camera position destination
        Vector3 direction = Quaternion.Euler(_targetRotation) * Vector3.back;
        Vector3 cameraDestination = _facingPoint + direction * _cameraDistance;

        if (_cameraMovementDampening != 0f)
            cameraDestination = Vector3.Lerp(_camera.position, cameraDestination, (1f - _cameraMovementDampening) * Time.deltaTime * 10f);

        // Move the camera to the new position
        _camera.position = cameraDestination;

        // And finally we can do collision and facing
        HandleCameraCollisionAndFacing(oldRotation);
    }

    private void HandleCameraCollisionAndFacing(Vector3 oldRotation)
    {
        Vector3 currentRotation = _camera.eulerAngles;
        _camera.LookAt(_facingPoint);

        if (Physics.Raycast(_facingPoint, _camera.rotation * Vector3.back, out RaycastHit hitInfo, Vector3.Distance(_camera.position, _facingPoint), _collisionLayerMask.value))
            _camera.position = hitInfo.point;

        // Make sure that our leaning angle stays within boundaries
        float angleX = Mathf.Clamp(Mathf.DeltaAngle(0f, currentRotation.x), _minCameraLeanAngle, _maxCameraLeanAngle);

        // We have locked onto a target. Lerp towards the target angle and increase the speed the longer it takes
        if (_lockFacingTimer.HasValue)
        {
            _lockFacingTimer += Time.deltaTime;
            _camera.rotation = Quaternion.Lerp(Quaternion.Euler(oldRotation), Quaternion.Euler(currentRotation), Mathf.Min(_lockFacingTimer.Value));
            if (_lockFacingTimer.Value >= 1f)
                _lockFacingTimer = null;
        }
        else
            _camera.rotation = Quaternion.Euler(angleX, currentRotation.y, currentRotation.z);
    }

    public void LockTarget(Transform target)
    {
        _lockFacingTimer = 0f;
        _lockTarget = target;
    }
}
