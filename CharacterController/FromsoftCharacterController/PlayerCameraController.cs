/*
* This file is part of the UnityComponents repository authored by Ovahlord (https://github.com/Ovahlord/UnityComponents)
* All components in this repository are royalty free and can be used for commercial purposes. Enjoy.
*/

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.UIElements;
using static UnityEngine.UI.Image;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Camera Following Settings")]
    [Tooltip("The dampening factor for the point that the camera is facing by default. The higher the value, the further this point falls behind the player")]
    [SerializeField][Range(0f, 1f)] private float _facingPointDampening = 0.2f;
    [Tooltip("The dampening factor for the camera movement. The higher the value, the longer it takes for the camera to each its destinatiin")]
    [SerializeField][Range(0f, 1f)] private float _cameraMovementDampening = 0.8f;
    [Tooltip("Increases the height of all points by the specified values. 2f means that the camera will be 2 meters above the transform's position")]
    [SerializeField][Min(0f)] private float _heightOffset = 2f;
    [Tooltip("The distance between the facing point and the camera")]
    [SerializeField][Min(1f)] private float _cameraDistance = 10f;
    [Tooltip("The layermask that the camera can collide with. If set, gameobjects on these layers will cause the camera to adjust its distance accordingly")]
    [SerializeField] private LayerMask _collisionLayerMask = 0;

    [Header("Camera Rotation Settings")]
    [Tooltip("The initial angle of the camera X axis at which the camera will be initialized")]
    [SerializeField][Range(-90f, 90f)] private float _initialCameraLearnAngle = 20f;
    [Tooltip("The lower bounds of the camera angle. The camera will not be able to rotate below the specified degrees on its X axis")]
    [SerializeField][Range(-89f, 0f)] private float _minCameraLeanAngle = -30;
    [Tooltip("The lower bounds of the camera angle. The camera will not be able to rotate above the specified degrees on its X axis")]
    [SerializeField][Range(0f, 90f)] private float _maxCameraLeanAngle = 60;
    [Tooltip("The X angle in degrees that the camera will fixate on when a target has been locked onto")]
    [SerializeField][Range(-90f, 90f)] private float _lockedCameraLeanAngle = 20f;

    [Header("Camera Target Locking")]
    [Tooltip("The layermask which lock target transforms must have in order to get selected")]
    [SerializeField] private LayerMask _targetLockingLayerMask = 0;
    [Tooltip("The maximum distance between player and targets. If a target has been locked onto and goes beyond the distance, the lock will get released")]
    [SerializeField] private float _targetLockingMaxDistance = 50f;
    [Tooltip("The amount of time in seconds which the player can restore the distance and line of sight between him and the locked target. If the timer expires, the lock will be released")]
    [SerializeField] private float _lostTargetRecoveryTime = 2f;
    [Tooltip("The speed multiplier at which the camera will turn towards its lock target. This value is multiplied against Time.deltaTime")]
    [SerializeField] private float _targetLockAngleAdaptionRate = 0.2f;

    private static PlayerCameraController _instance = null;
    private readonly RaycastHit[] _lockTargetHits = new RaycastHit[50]; // a buffer of 50 possible targets in one sphere cast should be more than enough

    public static Vector2 TurnInputValue { get; set; }
    public static float CameraYAngle { get { return _instance._camera.eulerAngles.y; } }
    public static bool IsLockedToTarget { get { return _instance._lockTarget != null; } }

    private bool _isUsingGamepad = false;
    private float? _lockFacingTimer = null;
    private float? _lostTargetRecoveryTimer = null;
    private Transform _camera = null;
    private Transform _lockTarget = null;
    private Vector3 _originPoint = Vector3.zero;
    private Vector3 _facingPoint = Vector3.zero;
    private Vector3 _targetRotation = Vector3.zero;

    private void Awake()
    {
        if (_instance != null)
            Destroy(_instance);

        _instance = this;
        DontDestroyOnLoad(this);

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

    private void FixedUpdate()
    {
        if (_lockTarget != null)
        {
            if (!ValidateLockTarget())
            {
                if (!_lostTargetRecoveryTimer.HasValue)
                    _lostTargetRecoveryTimer = _lostTargetRecoveryTime;
                else
                {
                    _lostTargetRecoveryTimer -= Time.fixedDeltaTime;
                    if (_lostTargetRecoveryTimer <= 0f)
                    {
                        ToggleCameraLock();
                        _lostTargetRecoveryTimer = null;
                    }
                }
            }
            else
                _lostTargetRecoveryTimer = null;
        }
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
        // Check for destination collision
        if (Physics.Linecast(_facingPoint, cameraDestination, out RaycastHit hitInfo, _collisionLayerMask.value))
            cameraDestination = hitInfo.point;

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

        if (Physics.Linecast(_facingPoint, _camera.position, out RaycastHit hitInfo, _collisionLayerMask.value))
            _camera.position = hitInfo.point;

        // Make sure that our leaning angle stays within boundaries
        float angleX = Mathf.Clamp(Mathf.DeltaAngle(0f, currentRotation.x), _minCameraLeanAngle, _maxCameraLeanAngle);

        // We have locked onto a target. Lerp towards the target angle and increase the speed the longer it takes
        if (_lockFacingTimer.HasValue)
        {
            _lockFacingTimer += Time.deltaTime * _targetLockAngleAdaptionRate;
            _camera.rotation = Quaternion.Lerp(Quaternion.Euler(oldRotation), Quaternion.Euler(currentRotation), Mathf.Min(_lockFacingTimer.Value));
            if (_lockFacingTimer.Value >= 1f)
                _lockFacingTimer = null;
        }
        else
            _camera.rotation = Quaternion.Euler(angleX, currentRotation.y, currentRotation.z);
    }

    public static void ToggleCameraLock()
    {
        // Release active lock
        if (_instance._lockTarget != null)
        {
            _instance._lockTarget = null;
            _instance._lockFacingTimer = 0f;
        }
        else
        {
            // Select new lock target
            _instance._lockTarget = _instance.SelectLockTarget();
            if (_instance._lockTarget != null)
                _instance._lockFacingTimer = 0f;
        }

        PlayerHUDController.SetLockOnTarget(_instance._lockTarget);
    }

    private Transform SelectLockTarget()
    {
        Array.Clear(_lockTargetHits, 0, _lockTargetHits.Length);
        int hitCount = Physics.SphereCastNonAlloc(transform.position, _targetLockingMaxDistance, Camera.main.transform.forward, _lockTargetHits, 0f, _targetLockingLayerMask.value);

        Transform closestTarget = null;
        for (int i = 0; i < hitCount; ++i)
        {
            Transform hitTransform = _lockTargetHits[i].transform;

            // Only allow targets that are in front of the player
            Vector3 directionToTarget = transform.position - hitTransform.position;
            float angle = Vector3.Angle(Camera.main.transform.rotation * Vector3.forward, directionToTarget);
            if (Mathf.Abs(angle) <= 90f)
                continue;

            if (Physics.Linecast(Camera.main.transform.position, hitTransform.position, _collisionLayerMask))
                continue;

            if (closestTarget == null)
            {
                closestTarget = hitTransform;
                continue;
            }

            float closestDist = Vector3.Distance(transform.position, closestTarget.position);
            if (Vector3.Distance(transform.position, hitTransform.position) <= closestDist)
                closestTarget = hitTransform;
        }

        return closestTarget;
    }

    private bool ValidateLockTarget()
    {
        if (_lockTarget == null)
            return false;

        // Ensure that the target remains within our range
        if (Vector3.Distance(transform.position, _lockTarget.position) > _targetLockingMaxDistance)
            return false;

        // Something came between the camera and the target. Interrupt targeting
        if (Physics.Linecast(Camera.main.transform.position, _lockTarget.position, _collisionLayerMask))
            return false;

        return true;
    }
}
