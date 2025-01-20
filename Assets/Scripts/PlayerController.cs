using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    public float moveSpeed = 10f;
    public float rotationSpeed = 5f;
    
    private Vector3 _moveDir;
    private PlayerInput _playerInput;
    private LayerMask _wallMask;
    private RaycastHit _hit;
    private Vector3 _boxExtent;

    private BoxCollider _boxCollider;
    private Camera _mainCamera;
    private Vector2 _mouseScreenPosition;
    private PlayerHealth _health;

    void Awake() {
        _playerInput = GetComponent<PlayerInput>();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        
        if (IsOwner) {
            _mainCamera = Camera.main;
            _wallMask = LayerMask.GetMask("Ground", "InnerWall");
            _boxExtent = transform.localScale / 2 - new Vector3(0, .25f, 0);
            _boxCollider = GetComponent<BoxCollider>();
            _health = GetComponent<PlayerHealth>();
            return;
        }
        _playerInput.enabled = false;
    }

    void Update() {
        if (!IsOwner || !IsSpawned || !_health.IsAlive()) return; // Ensure only the local player controls their own character, and they are alive

        Movement();
        Rotation();
    }
    
    void OnMove(InputValue value) {
        if (!IsOwner) return; // Ensure only the local player controls their own character
        
        Vector2 inputVector = value.Get<Vector2>();
        _moveDir = new Vector3(inputVector.x, 0, inputVector.y);
    }
    
    void OnLook(InputValue value) {
        _mouseScreenPosition = value.Get<Vector2>();
    }

    void Movement() {
        Vector3 newPos = _moveDir * (moveSpeed * Time.deltaTime);
        
        // Prevent wall clipping
        if (Physics.CheckBox(transform.position + 2 * newPos, _boxExtent, transform.rotation, _wallMask)) {
            if (Physics.BoxCast(transform.position, _boxExtent, newPos, out _hit, transform.rotation, newPos.magnitude * 2, _wallMask)) 
            {
                Vector3 slideDir = newPos - Vector3.Dot(newPos, _hit.normal) * _hit.normal; // Project movement vector onto the wall surface (remove the normal component)
                Vector3 shiftedPos = slideDir * (moveSpeed * Time.deltaTime); // Apply movement along the slide direction
                
                Debug.DrawRay(_hit.point, _hit.normal, Color.green, 3f);
                Debug.DrawRay(transform.position, shiftedPos, Color.yellow, 3f);
                
                transform.position += shiftedPos;
                return;
            }
        }
        
        transform.position += newPos;
    }
    
    void Rotation()
    {
        // Convert mouse screen position to world position
        Vector3 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(new Vector3(_mouseScreenPosition.x, _mouseScreenPosition.y, _mainCamera.transform.position.y));

        // Calculate direction
        Vector3 direction = (mouseWorldPosition - transform.position).normalized;
        direction.y = 0; // Keep the player level on the Y-axis
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Projected rotation based on movement
        Quaternion projectedRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // Check for wall overlap using the projected rotation
        Collider[] collisions = Physics.OverlapBox(transform.position, _boxExtent, projectedRotation, _wallMask);

        // If the new rotation wouldn't cause the player to collide with walls, just apply the rotation and return out
        if (collisions.Length == 0) { 
            transform.rotation = projectedRotation;
            return;
        }

        // If the new rotation will collide with walls, push the player away from the wall
        Vector3 totalPushback = Vector3.zero;
        foreach (Collider collider in collisions) {
            bool overlapping = Physics.ComputePenetration(
                _boxCollider, transform.position, projectedRotation, // Rotated collider
                collider, collider.transform.position, collider.transform.rotation, // Colliding object
                out Vector3 pushDirection, out float penetrationDepth);
            
            if (overlapping) 
                totalPushback += pushDirection * penetrationDepth; // Push back in the direction opposite to penetration
        }
        // Apply the pushback if necessary
        if (totalPushback != Vector3.zero)
            transform.position += totalPushback;

        // Apply the rotation
        transform.rotation = projectedRotation;
    }
}