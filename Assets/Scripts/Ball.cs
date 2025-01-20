using System;
using System.Net;
using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;

public class Ball : NetworkBehaviour {
    public float initialSpeed = 5f;
    public float bounceSpeedIncrease = 1f;
    private Rigidbody _rigidbody;
    
    private float _ballTimout = 5f;
    private float _ballTimer = 0;
    
    private void Awake() {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn() {
        if (IsServer) {
            LaunchBall(Vector3.forward); // Initial direction, replace with actual logic later
        }
    }

    private void Update() {
        _ballTimer += Time.deltaTime;
        if (_ballTimer >= _ballTimout)
            ResetBallServerRpc();
    }

    public void LaunchBall(Vector3 direction) {
        _rigidbody.linearVelocity = direction.normalized * initialSpeed;
        UpdateVelocityClientRpc(_rigidbody.linearVelocity);
    }

    private void OnCollisionEnter(Collision collision) {
        if (!IsServer || collision.gameObject.CompareTag("Catcher")) return; // Only the server handles physics and synchronization. Don't bounce on the catcher
        
        Vector3 incomingVelocity = _rigidbody.linearVelocity;
        _rigidbody.linearVelocity = incomingVelocity.normalized * (incomingVelocity.magnitude + bounceSpeedIncrease);
        
        //Debug.Log($"Total Velocity: {outgoingVelocity.magnitude}");
        //Debug.DrawRay(collision.GetContact(0).point, collision.GetContact(0).normal, Color.red, 50f); //the bounce's normal vector
        //Debug.DrawRay(transform.position, _rigidbody.linearVelocity, Color.blue, 50f); //new direction
        
        if (collision.gameObject.TryGetComponent(out PlayerIdentifier pID)) {
            if (pID.GetID != GameManager.Instance.GetThrower())
                collision.gameObject.GetComponent<PlayerHealth>().DealDamage();
        }
        
        UpdateVelocityClientRpc(_rigidbody.linearVelocity); // Synchronize the ball's new velocity across all clients
    }

    [ClientRpc]
    private void UpdateVelocityClientRpc(Vector3 newVelocity) {
        if (IsServer) return; // Server already has the correct velocity

        _rigidbody.linearVelocity = newVelocity;
    }
    
    [ServerRpc]
    private void ResetBallServerRpc() {
        // Despawn ball & give possession to another player
        NetworkObject ballNetObj = GetComponent<NetworkObject>();
        ballNetObj.Despawn();
        AssignNewOwnerToClosestPlayer();
    }
    
    private void AssignNewOwnerToClosestPlayer() {
        PlayerIdentifier[] players = FindObjectsByType<PlayerIdentifier>(FindObjectsSortMode.InstanceID);
        if (players.Length == 0) return;

        // Find the closest player to the ball
        PlayerIdentifier closestPlayer = null;
        float minDistance = float.MaxValue;

        foreach (var player in players) {
            if (players.Length > 1 && player.GetID == GameManager.Instance.GetThrower()) continue;
            
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < minDistance) {
                minDistance = distance;
                closestPlayer = player;
            }
        }
        if (closestPlayer != null) {
            Debug.Log($"New thrower selected: {closestPlayer.OwnerClientId}");

            // Target only the new thrower
            ClientRpcParams clientRpcParams = new ClientRpcParams {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { closestPlayer.OwnerClientId } }
            };

            closestPlayer.GetComponent<BallHandler>().ReceiveBall(clientRpcParams);
        }
    }
}