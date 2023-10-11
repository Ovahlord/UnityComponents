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
    private class TargetLockFacingData
    {
        public float ElapsedTime = 0f;
        public Quaternion OriginalRotation = Quaternion.identity;
    }

    [Header("Camera Following Settings")]
    [Tooltip("The dampening factor for the point that the camera is facing by default. The higher the value, the further this point falls behind the player")]
    [SerializeField][Range(0f, 1f)] private float _facingPointDampening = 0.5f;
    [Tooltip("The dampening factor for the camera movement. The higher the value, the longer it takes for the camera to each its destinatiin")]
    [SerializeField][Range(0f, 1f)] private float _cameraMovementDampening = 0.5f;
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
    [SerializeField][Range(0f, 10f)] private float _targetLockAngleAdaptionRate = 5f;

    private static PlayerCameraController _instance = null;
    private readonly RaycastHit[] _lockTargetHits = new RaycastHit[50]; // a buffer of 50 possible targets in one sphere cast should be more than enough

    public static Vector2 TurnInputValue { get; set; }
    public static float CameraYAngle { get { return _instance._camera.eulerAngles.y; } }
    public static bool IsLockedToTarget { get { return _instance._lockTarget != null; } }

    private bool _isUsingGamepad = false;
    private float? _lostTargetRecoveryTimer = null;
    private TargetLockFacingData _lockFacingData = null;
    private Transform _camera = null;
    private Transform _lockTarget = null;
    private Vector3 _originPoint = Vector3.zero;
    private Vector3 _facingPoint = Vector3.zero;
    private Vector3 _targetRotation = Vector3.zero;
    private Vector3 _rawCameraPosition = Vector3.zero; // proposed camera destination without any collision checks

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
        _rawCameraPosition = _camera.position;
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
        // Apply the difference between our current and our target rotation. Ignore the X axis because that one is fixed and can only be changed by input.
        _camera.LookAt(_lockTarget != null ? _lockTarget.position : _facingPoint);
        _targetRotation.y = _camera.eulerAngles.y;
        _targetRotation.z = _camera.eulerAngles.z;
        if (_lockTarget != null) // locked targets enforce a fixed lean angle
            _targetRotation.x = _lockedCameraLeanAngle;

        // Calculate our raw camera destination
        Vector3 direction = Quaternion.Euler(_targetRotation) * Vector3.back;
        Vector3 cameraDestination = _facingPoint + direction * _cameraDistance;
        if (_cameraMovementDampening != 0f)
            _rawCameraPosition = Vector3.Lerp(_rawCameraPosition, cameraDestination, (1f - _cameraMovementDampening) * Time.deltaTime * 10f);
        else
            _rawCameraPosition = cameraDestination;

        // Next we will rotate our camera based on our input
        if (_lockTarget == null)
            HandleCameraTurnInput();

        // Now we can relocate the camera
        if (Physics.Linecast(_facingPoint, _rawCameraPosition, out RaycastHit hitInfo, _collisionLayerMask.value))
            _camera.position = hitInfo.point;
        else
            _camera.position = _rawCameraPosition;

        // And now we hande
        HandleCameraFacing();
    }

    private void HandleCameraTurnInput()
    {
        if (TurnInputValue.magnitude == 0f)
            return;

        // First we check our target destination for the camera
        Vector3 currentDestination = Quaternion.Euler(_targetRotation) * (Vector3.back * _cameraDistance);

        // Apply rotation offsets to our target rotation
        Vector2 turnOffset = GetInputOffsets();
        _targetRotation.x = Mathf.Clamp(Mathf.DeltaAngle(0f, _targetRotation.x) - turnOffset.y, _minCameraLeanAngle, _maxCameraLeanAngle);
        _targetRotation.y += turnOffset.x;

        // Now we calculate the destination again and apply the delta between the two points to the raw destination
        Vector3 turnedDestination = Quaternion.Euler(_targetRotation) * (Vector3.back * _cameraDistance);
        _rawCameraPosition += turnedDestination - currentDestination;
    }

    private void HandleCameraFacing()
    {
        // Default behavior: just face the target
        float angleX = 0f;
        if (_lockFacingData == null)
        {
            _camera.LookAt(_lockTarget != null ? _lockTarget.position : _facingPoint);
            // Never allow the lean angle to go beyond our limitations
             angleX = Mathf.Clamp(Mathf.DeltaAngle(0f, _camera.eulerAngles.x), _minCameraLeanAngle, _maxCameraLeanAngle);
            _camera.rotation = Quaternion.Euler(angleX, _camera.eulerAngles.y, _camera.eulerAngles.z);
            return;
        }

        // Lerp towards our target
        _camera.LookAt(_lockTarget != null ? _lockTarget.position : _facingPoint);
        _lockFacingData.ElapsedTime += Time.deltaTime * _targetLockAngleAdaptionRate;
        _camera.rotation = Quaternion.Lerp(_lockFacingData.OriginalRotation, _camera.rotation, _lockFacingData.ElapsedTime);
        if (_lockFacingData.ElapsedTime >= 1f)
            _lockFacingData = null;
    }

    public static void ToggleCameraLock()
    {
        TargetLockFacingData data = null;

        if (_instance._lockTarget != null)
        {
            _instance._lockTarget = null;
            data = _instance._lockFacingData = new();
        }
        else
        {
            // Select new lock target
            _instance._lockTarget = _instance.SelectLockTarget();
            if (_instance._lockTarget != null)
                data = _instance._lockFacingData = new();
        }

        if (data != null)
            data.OriginalRotation = _instance._camera.rotation;

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
