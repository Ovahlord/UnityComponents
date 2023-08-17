/*
* This file is part of the UnityComponents repository authored by Ovahlord (https://github.com/Ovahlord/UnityComponents)
* All components in this repository are royalty free and can be used for commercial purposes. Enjoy.
*/

using UnityEngine;

public class SpringArmCollisionContext
{
    public SpringArmCollisionContext(Vector3 collisionHitPoint, Transform hitTransform)
    {
        CollisionHitPoint = collisionHitPoint;
        HitTransform = hitTransform;
    }

    public Vector3 CollisionHitPoint { get; }
    public Transform HitTransform { get; }
}

public class SpringArm : MonoBehaviour
{
    private enum SpringArmUpdateMethod
    {
        OnUpdate        = 0,
        OnFixedUpdate   = 1,
        OnLateUpdate    = 2
    }

    [Tooltip("The maximum length of the spring arm. The attached transform will be relocated to the tip of the arm")]
    [SerializeField][Min(0f)] private float _armLength = 0f;
    [Tooltip("The transform that will be relocated to the tip of the arm")]
    [SerializeField] private Transform _attachment = null;
    [Tooltip("The direction that the tip of the arm will be facing. For example (0, 0, -1) will make the arm reach backward which is often used for camera attachments")]
    [SerializeField] private Vector3 _direction = Vector3.zero;
    [Tooltip("The layers that the spring arm will collide with and adjust its distance to it")]
    [SerializeField] private LayerMask _collisionLayerMask = 0;
    [Tooltip("Makes the attachment face towards the origin point of the arm (the position of the transform this component is attached to)")]
    [SerializeField] private bool _faceOrigin = true;
    [Tooltip("Defines the update method that is being used to update the spring arm length and its attached transform's position")]
    [SerializeField] private SpringArmUpdateMethod _updateMethod = SpringArmUpdateMethod.OnUpdate;

    public delegate void CollisionEventHandler(object sender, SpringArmCollisionContext context);
    public event CollisionEventHandler OnCollide;

    public void SetAttachment(Transform attachment) { _attachment = attachment; }
    public void SetArmLength(float length) { _armLength = length; }
    public void SetDirection(Vector3 direction) { _direction = direction; }
    public void SetFaceToOrigin(bool enable) { _faceOrigin = enable; }
    public void SetCollisionLayerMask(LayerMask collisionLayerMask) { _collisionLayerMask = collisionLayerMask; }

    private void OnValidate()
    {
        UpdateTransformPosition();
    }

    // Start is called before the first frame update
    void Start()
    {
        UpdateTransformPosition();
    }

    // Update is called once per frame
    void Update()
    {
        if (_updateMethod == SpringArmUpdateMethod.OnUpdate)
            UpdateTransformPosition();
    }

    void FixedUpdate()
    {
        if (_updateMethod == SpringArmUpdateMethod.OnFixedUpdate)
            UpdateTransformPosition();
    }

    void LateUpdate()
    {
        if (_updateMethod == SpringArmUpdateMethod.OnLateUpdate)
            UpdateTransformPosition();
    }

    private void UpdateTransformPosition()
    {
        if (_attachment == null)
            return;

        if (_armLength == 0f)
        {
            _attachment.position = transform.position;
            return;
        }

        Vector3 targetPosition = transform.position + transform.TransformDirection(_direction) * _armLength;

        // Perform a raycast when we are suposed to collide with something and adjust the target point.
        if (_collisionLayerMask != 0)
        {
            if (Physics.Raycast(transform.position, transform.TransformDirection(_direction), out RaycastHit hit, _armLength, _collisionLayerMask.value))
            {
                OnCollide?.Invoke(this, new SpringArmCollisionContext(hit.point, hit.transform));
                targetPosition = hit.point;
            }
        }

        _attachment.position = targetPosition;

        if (_faceOrigin)
            _attachment.LookAt(transform.position);
    }
}
