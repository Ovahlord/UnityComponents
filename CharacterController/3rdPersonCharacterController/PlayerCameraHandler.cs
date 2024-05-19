using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerCameraHandler : MonoBehaviour
{
    [Header("General Settings")]
    [Tooltip("The camera that will be used. If left empty the Camera with the Tag 'MainCamera' will be used.")]
    [SerializeField] private Transform _cameraOverride = null;
    [Tooltip("The target the camera will follow. If left empty this component's GameObject will be the target.")]
    [SerializeField] private Transform _targetOverride = null;
    [Tooltip("The maximum angle in degrees which the camera can lean forward and backward")]
    [SerializeField, Range(0f, 60f)] private float _maxLeanAngleDelta = 60f;
    [SerializeField, Min(0f)] private float _initialLeanAngle = 20f;
    [SerializeField, Min(0f)] private float _cameraDistance = 7f;
    [SerializeField] private float _heightOffset = 2f;
    [Tooltip("The collision layers that will cause the camera to move in front of colliding objects between player and object to avoid obscuring his vision")]
    [SerializeField] private LayerMask _cameraCollisionLayerMask = 1;
    [Header("Input Device Settings")]
    [SerializeField, Min(0f)] private float _mouseSensitivity = 0.4f;
    [Tooltip("For Gamepads only. The speed in degress per second at which the player can rotate the camera")]
    [SerializeField, Min(0f)] private float _turnRate = 360f;
    [Tooltip("The name of the InputActions control scheme for the Gamepad. This is being used to distinguish between Mouse and Keyboard and Gamepads")]
    [SerializeField] private string _gamepadControlSchemeName = "Gamepad";
    [Tooltip("The event that provides a PlayerMovementHandler component with camera angle data. The PlayerMovementHandler function that should subscribe to this event is 'SetHorizontalViewDirection'")]
    [SerializeField] private UnityEvent<Quaternion> OnHorizontalViewDirectionChanged = null;

    public bool InputDisabled
    {
        get { return _inputDisabled; }
        set
        {
            _inputDisabled = value;
            _lookInputValue = Vector2.zero;
        }
    }

    private bool _inputDisabled = false;

    private PlayerInput _playerInput = null;
    private Transform _cameraTransform = null;
    private Transform _targetTransform = null;
    private Vector3 _rotation = Vector2.zero;
    private Vector2 _lookInputValue = Vector2.zero;
    private float _targetCameraDistance = 0f;

    private void Awake()
    {
        _initialLeanAngle = Mathf.Min(_initialLeanAngle, _maxLeanAngleDelta);
        _targetCameraDistance = _cameraDistance;

        _playerInput = GetComponent<PlayerInput>();
        _cameraTransform = _cameraOverride != null ? _cameraOverride : Camera.main.transform;
        _targetTransform = _targetOverride != null ? _targetOverride : transform;

        _rotation = new(_initialLeanAngle, _targetTransform.eulerAngles.y, _targetTransform.eulerAngles.z);
    }

    private void Start()
    {
        // Let's start with a first camera position update
        UpdateCameraPositionAndRotation();
    }

    // We are going to update the camera in LateUpdate to ensure that the target position has been updated first in Update()
    private void LateUpdate()
    {
        UpdateCameraPositionAndRotation();
    }

    private void UpdateCameraPositionAndRotation()
    {
        // Apply the rotation input delta to our locally stored rotation value first (we will sanitize it in the next step)
        if (_lookInputValue.magnitude != 0f)
        {
            if (_playerInput.currentControlScheme != _gamepadControlSchemeName)
            {
                // Mouse and Keyboard case - apply delta value and reset it afterwards
                _rotation.x -= _lookInputValue.y;
                _rotation.y += _lookInputValue.x;
                _lookInputValue.Set(0f, 0f);
            }
            else
            {
                _rotation.x -= _lookInputValue.y * _turnRate * Time.deltaTime;
                _rotation.y += _lookInputValue.x * _turnRate * Time.deltaTime;
            }

            _rotation.x = Mathf.Clamp(_rotation.x, -_maxLeanAngleDelta, _maxLeanAngleDelta);
        }

        Quaternion rotation = Quaternion.Euler(_rotation);
        _rotation.y = rotation.eulerAngles.y; // just a means to avoid over- or underflows if we turn the y axis too ofte

        Vector3 origin = _targetTransform.position + Vector3.up * _heightOffset;

        if (Physics.SphereCast(origin, 0.1f, rotation * Vector3.back, out RaycastHit hitInfo, _cameraDistance, _cameraCollisionLayerMask.value))
            _targetCameraDistance = hitInfo.distance;
        else
            _targetCameraDistance = _cameraDistance;

        _cameraTransform.position = origin + rotation * Vector3.back * _targetCameraDistance;
        _cameraTransform.LookAt(origin);
        OnHorizontalViewDirectionChanged?.Invoke(Quaternion.Euler(0f, _rotation.y, 0f));
    }

    // Input Messages
    public void OnLook(InputValue value)
    {
        if (InputDisabled)
            return;

        Vector2 inputValue = value.Get<Vector2>();
        if (_playerInput.currentControlScheme != _gamepadControlSchemeName)
        {
            // Mouse and Keyboard case - sum up all the delta input values. They are screen pixel offsets so we divide them with the screen size
            _lookInputValue += inputValue * _mouseSensitivity;
        }
        else
        {
            // Gamepad case - just store the current input value. It's a normalized stick delta value
            _lookInputValue = inputValue;
        }
    }
}
