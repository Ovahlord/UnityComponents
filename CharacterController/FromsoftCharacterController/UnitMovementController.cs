/*
* This file is part of the UnityComponents repository authored by Ovahlord (https://github.com/Ovahlord/UnityComponents)
* All components in this repository are royalty free and can be used for commercial purposes. Enjoy.
*/

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class UnitMovementController : MonoBehaviour
{
    public enum MovementType
    {
        Walk    = 0,
        Run     = 1,
        Sprint  = 2
    }

    [SerializeField] private float _walkSpeed   = 5f;
    [SerializeField] private float _runSpeed    = 10f;
    [SerializeField] private float _sprintSpeed = 15f;
    [SerializeField] private float _turnSpeed   = 720f;

    private CharacterController _characterController = null;
    public Vector3 MotionDirection { get; set; } = Vector3.zero;
    public MovementType MotionType { get; set; } = MovementType.Walk;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (MotionDirection.magnitude == 0f)
            return;

        _characterController.Move(MotionDirection * (GetMovementSpeed() * Time.deltaTime));
        float y = Mathf.MoveTowardsAngle(transform.eulerAngles.y, Mathf.Atan2(MotionDirection.x, MotionDirection.z) * Mathf.Rad2Deg, _turnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, y, 0f);
    }

    private float GetMovementSpeed()
    {
        return MotionType switch
        {
            MovementType.Walk => _walkSpeed,
            MovementType.Run => _runSpeed,
            MovementType.Sprint => _sprintSpeed,
            _ => 0f
        };
    }
}
