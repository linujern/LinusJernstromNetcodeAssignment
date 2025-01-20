using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerRotation : NetworkBehaviour
{
    public float rotationSpeed = 10f;
    
    private Camera _mainCamera;
    private Vector2 _mouseScreenPosition;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (!IsOwner) return;
        
        _mainCamera = Camera.main;
    }

    void Update() {
        if (!IsOwner) return;

        // Convert mouse screen position to world position
        Vector3 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(new Vector3(_mouseScreenPosition.x, _mouseScreenPosition.y, _mainCamera.transform.position.y));
        
        // Calculate direction and rotate the player
        Vector3 direction = (mouseWorldPosition - transform.position).normalized;
        direction.y = 0; // Keep the player level on the Y-axis
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void OnLook(InputValue value) {
        _mouseScreenPosition = value.Get<Vector2>();
    }
}
