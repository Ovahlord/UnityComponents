using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
public class PlayerMovementHandler : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _walkSpeed = 3.5f;
    [SerializeField, Min(0f)] private float _runSpeed = 7f;
    [SerializeField, Min(0f)] private float _sprintSpeed = 12f;
    [Tooltip("The maximum jump height in meters. When jumping, the controller will move up to that specified height before falling back down.")]
    [SerializeField, Min(0f)] private float _maxJumpHeight = 5f;
    [Tooltip("The gravity force that pulls the gameobject back to the ground. The higher the value, the shorter the jump distance will becone.")]
    [SerializeField, Min(1f)] private float _gravity = 40f;
    [Tooltip("The maximum fall speed in meters per second.")]
    [SerializeField, Min(1f)] private float _maxFallSpeed = 20f;
    [Tooltip("The speed in degrees per second at which the transform will rotate towards its move direction.")]
    [SerializeField, Min(1f)] private float _steeringRate = 720f;

    public bool InputDisabled
    {
        get { return _inputDisabled; }
        set
        {
            _inputDisabled = value;
            _isSprinting = false;
            _moveInputValue = Vector3.zero;
        }
    }

    private bool _inputDisabled = false;

    private CharacterController _characterController = null;
    private Vector3 _moveInputValue = Vector3.zero;
    private Vector3? _fallVelocity = null;
    private bool _isSprinting = false;

    // Ground Data
    private Vector3 _groundNormal = Vector3.zero;
    private Vector3 _groundContactPoint = Vector3.zero;
    private bool _isOnSteepGround = false;

    private Quaternion _horizontalCameraViewDirection = Quaternion.identity;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        Debug.Assert(_characterController != null);
    }

    private void Update()
    {
        // If we are currently falling, update the velocity such as applying gravity
        if (_fallVelocity.HasValue)
            UpdateFallVelocity();

        Vector3 motion = CalculateMotion();
        if (motion.magnitude < _characterController.minMoveDistance)
            return;

        float previousYPosition = _characterController.transform.position.y;
        _characterController.Move(motion);

        HandlePostMoveActions(previousYPosition);

        if (motion.x != 0f && motion.z != 0f)
        {
            // Update the facing only when moving horizontally (vertical fall may return 0, 0, 0 which can result in unwanted facing directions)
            float targetYaw = Mathf.Atan2(motion.x, motion.z) * Mathf.Rad2Deg;
            transform.Rotate(Vector3.up, Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetYaw, _steeringRate * Time.deltaTime) - transform.eulerAngles.y);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        _groundNormal = hit.normal;
        _groundContactPoint = hit.point;
        _isOnSteepGround = IsOnSteepFloor();
    }

    private bool IsOnSteepFloor()
    {
        // First we will check the normal if the ground that we just hit. If that one returns a valid angle, we don't have to care about the rest anymore.
        if (Vector3.Angle(_groundNormal, Vector3.up) < _characterController.slopeLimit)
            return false;

        // Due to PhysX suffering from a precision issue when walking on edges, we have to perform a top down raycast on a surface first to ensure that we don't fall for a false positive
        if (Physics.Raycast(_groundContactPoint + Vector3.up * 0.05f, Vector3.down, out RaycastHit hitInfo, 1f))
            if (Vector3.Angle(hitInfo.normal, Vector3.up) < _characterController.slopeLimit)
                return false;

        return true;
    }

    private void UpdateFallVelocity()
    {
        if (!_fallVelocity.HasValue)
            return;

        // Apply gravity
        _fallVelocity += Vector3.down * (_gravity * Time.deltaTime);
    }

    private Vector3 CalculateMotion()
    {
        if (_fallVelocity.HasValue)
        {
            // Fall is active, continue to fall
            return _fallVelocity.Value * Time.deltaTime;
        }
        else
        {
            float GetSpeed()
            {
                if (_isSprinting)
                    return _sprintSpeed;

                if (_moveInputValue.magnitude > 0.5f)
                    return _runSpeed;

                return _walkSpeed;
            }

            return (_horizontalCameraViewDirection * _moveInputValue.normalized) * (GetSpeed() * Time.deltaTime) + Vector3.down * _characterController.stepOffset;
        }
    }

    private void HandlePostMoveActions(float previousYPosition)
    {
        bool steppingUp = previousYPosition <= _characterController.transform.position.y;

        if (_characterController.isGrounded)
        {
            // We are grounded. Let's check our falling status and update/end it based on the circumstances
            if (_fallVelocity.HasValue)
            {
                if (!_isOnSteepGround)
                    _fallVelocity = null;
                else if (_fallVelocity.Value.y <= 0f)
                    _fallVelocity = Vector3.ProjectOnPlane(_fallVelocity.Value, _groundNormal);

            }
            else if (_isOnSteepGround && !steppingUp)
                _fallVelocity = _characterController.velocity;
        }
        else if (!_fallVelocity.HasValue)
        {
            // We are not grounded so lets start falling into the direction that we have last moved
            _fallVelocity = new(_characterController.velocity.x, 0f, _characterController.velocity.z);
            transform.position = new(transform.position.x, previousYPosition, transform.position.z);
        }

        // If we are colliding with a ceiling while jumping upwards, we will negate any upwards force so that gravity can start doing its job
        if (_fallVelocity.HasValue && _fallVelocity.Value.y > 0f && _characterController.collisionFlags.HasFlag(CollisionFlags.Above))
            _fallVelocity = new(_fallVelocity.Value.x, 0f, _fallVelocity.Value.z);
    }

    // Event Functions
    public void SetHorizontalViewDirection(Quaternion direction)
    {
        _horizontalCameraViewDirection = direction;
    }

    // Input Messages
    public void OnMove(InputValue value)
    {
        if (InputDisabled) 
            return;

        Vector2 moveValue = value.Get<Vector2>();
        _moveInputValue.Set(moveValue.x, 0f, moveValue.y);
    }

    public void OnJump(InputValue value)
    {
        if (InputDisabled)
            return;

        // A jump/fall is currently in progress - do not allow any meddling with it right now
        if (_fallVelocity.HasValue)
            return;

        if (_moveInputValue.magnitude != 0f)
            _fallVelocity = new(_characterController.velocity.x, 0f, _characterController.velocity.z);
        else
            _fallVelocity= Vector3.zero;

        _fallVelocity += Vector3.up * Mathf.Sqrt(_gravity * _maxJumpHeight * 2f);
    }

    public void OnSprint(InputValue value)
    {
        if (InputDisabled)
            return;

        _isSprinting = value.isPressed;
    }
}
