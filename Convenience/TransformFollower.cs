/*
* This file is part of the UnityComponents repository authored by Ovahlord (https://github.com/Ovahlord/UnityComponents)
* All components in this repository are royalty free and can be used for commercial purposes. Enjoy.
*/

using UnityEngine;

public class TransformFollower : MonoBehaviour
{
    [Tooltip("The target that we are going to follow.")]
    [SerializeField] private Transform _target = null;
    [Tooltip("The amount of dampening. 1 = no dampening, anything below 1 causes a smooth follow movement")]
    [SerializeField][Range(0f, 1f)] private float _catchupFactor = 1f;
    [Tooltip("Adds a offset to the destination position of the follower")]
    [SerializeField] private Vector3 _destinationOffset = Vector3.zero;
    [Tooltip("Causes the follower to be placed at the target's position on start.")]
    [SerializeField] private bool _snapToTargetOnStart = true;

    public void SetDestinationOffset(Vector3 offset) { _destinationOffset = offset; }
    public void SetTarget(Transform target) { _target = target; }
    public void SetCatchupFactor(float value) { _catchupFactor = Mathf.Clamp(_catchupFactor, 0f, 1f); }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Assert(_target != null, "A TransformFollower component does not have a target to follow. Component will not work.");

        if (_snapToTargetOnStart)
            transform.position = _target.TransformPoint(_destinationOffset);
    }

    // Update is called once per frame
    void Update()
    {
        // Catchup factor is set to 0 which implies no movement at all
        if (_catchupFactor == 0f)
            return;

        if (_catchupFactor == 1f) // No dampening, let's make it fast and efficient
            transform.position = _target.TransformPoint(_destinationOffset);
        else
            transform.position = Vector3.Lerp(transform.position, _target.position + _destinationOffset, _catchupFactor * Time.deltaTime * 10);
    }
}
