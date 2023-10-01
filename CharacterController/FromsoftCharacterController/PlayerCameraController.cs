/*
* This file is part of the UnityComponents repository authored by Ovahlord (https://github.com/Ovahlord/UnityComponents)
* All components in this repository are royalty free and can be used for commercial purposes. Enjoy.
*/

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Camera Following Settings")]
    [SerializeField][Range(0f, 1f)] private float _dampeningFactor = 0.5f;
    [SerializeField][Min(0f)] private float _heightOffset = 2f;
    [SerializeField] private LayerMask _collisionLayerMask = 0;
    public float CameraDistance = 10f;

    [Header("Camera Rotation Settings")]
    [SerializeField][Range(-90f, 90f)] private float _initalCameraAngle = 20f;
    [SerializeField][Range(-89f, 0f)] private float _minCameraLeanAngle = -30;
    [SerializeField][Range(0f, 90f)] private float _maxCameraLeanAngle = 60;
    public Vector2 TurnValue { get; set; }
    public bool IsMoving { get; set; }
    public float CameraYAngle { get { return _camera.eulerAngles.y; } }

    private Transform _camera = null;
    private Vector3 _destinationTransformPosition = Vector3.zero;
    private float _xAngle = 0f;
    private bool _isUsingGamepad = false;

    private void Awake()
    {
        InputUser.onChange += (InputUser user, InputUserChange change, InputDevice device) =>
        {
            if (change == InputUserChange.ControlSchemeChanged)
                _isUsingGamepad = user.controlScheme.Value.name.Equals("Gamepad");
        };

        _camera = Camera.main.transform;

        Quaternion initialRotation = Quaternion.Euler(_initalCameraAngle, transform.eulerAngles.y, transform.eulerAngles.z);
        _camera.SetPositionAndRotation(initialRotation * (Vector3.back * CameraDistance), initialRotation);
        _destinationTransformPosition = transform.position + Vector3.up * _heightOffset;
        _xAngle = _initalCameraAngle;
    }

    private void LateUpdate()
    {
        UpdateCameraPosition();
    }

    private void UpdateCameraPosition()
    {
        // First we lerp towards our target position
        if (_dampeningFactor != 0f)
            _destinationTransformPosition = Vector3.Lerp(_destinationTransformPosition, transform.position + Vector3.up * _heightOffset, Time.deltaTime * (1f - _dampeningFactor) * 10);
        else
            _destinationTransformPosition = transform.position + Vector3.up * _heightOffset;

        // Now we rotate the camera face towards our destination
        _camera.LookAt(_destinationTransformPosition);

        float distance;

        // Apply turn input
        if (TurnValue.magnitude != 0f)
        {
            float turnX = TurnValue.x;
            float turnY = TurnValue.y;
            if (_isUsingGamepad)
            {
                turnX *= Time.deltaTime;
                turnY *= Time.deltaTime;
            }

            _xAngle = Mathf.Clamp(_xAngle - turnY, _minCameraLeanAngle, _maxCameraLeanAngle);

            // Calculate new position
            Vector3 angle = _camera.eulerAngles;
            angle.y += turnX;
            angle.x -= turnY;

            // engine calculated euler angles are within the range of 0 - 360 degrees. We want negative degree values though so we use the delta towards 0 degrees instead
            angle.x = Mathf.Clamp(Mathf.DeltaAngle(0f, angle.x), _minCameraLeanAngle, _maxCameraLeanAngle);
            distance = Vector3.Distance(_camera.position, _destinationTransformPosition);

            if (Physics.Raycast(_destinationTransformPosition, Quaternion.Euler(angle) * Vector3.back, out RaycastHit hit, distance, _collisionLayerMask.value))
                _camera.position = hit.point;
            else
                _camera.position = _destinationTransformPosition + Quaternion.Euler(angle) * (Vector3.back * distance);
        }

        // Update the angles once more
        _camera.LookAt(_destinationTransformPosition);

        // Now we can apply the 2nd lerp to our actual camera position
        Vector3 direction = Quaternion.Euler(_xAngle, _camera.eulerAngles.y, _camera.eulerAngles.z) * Vector3.back;
        Vector3 destination = _destinationTransformPosition + direction * CameraDistance;

        destination = Vector3.Lerp(_camera.position, destination, Time.deltaTime * (1f - _dampeningFactor) * 10);
        distance = Vector3.Distance(_destinationTransformPosition, destination);

        if (Physics.Raycast(_destinationTransformPosition, direction, out RaycastHit hitInfo, distance, _collisionLayerMask.value))
            _camera.position = hitInfo.point;
        else
            _camera.position = destination;

        // Update the angles once more
        _camera.LookAt(_destinationTransformPosition);
    }
}
