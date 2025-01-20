using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class BallHandler : NetworkBehaviour
{
    [SerializeField] private Transform ItemSlot;
    [SerializeField] private GameObject _ballPrefab;
    
    private float _catchWindow = 0.2f;
    private float _catchAttemptCooldown = 1f;
    private float _throwTimeout = 5f;

    private NetworkVariable<bool> _hasBall = new NetworkVariable<bool>();

    private bool _isCatching = false;
    private float _catchAttemptTimer = 0;
    private float _windowCounter = 0;
    private float _throwTimer = 0;

    private NetworkVariable<bool> _isThrower = new NetworkVariable<bool>();
    
    private void OnInteract(InputValue value) {
        if (_hasBall.Value) {
            ThrowBallServerRpc();
            return;
        }
        
        if (_catchAttemptTimer >= _catchAttemptCooldown) { 
            _isCatching = true; 
        }
        else {
            //play sound
        }
    }
    
    private void OnTriggerEnter(Collider other) {
        if (!IsOwner) return; // Only the owner should handle catching logic

        if (other.CompareTag("Ball") && _isCatching) {
            TryCatchServerRpc(other.gameObject.GetComponent<NetworkObject>().NetworkObjectId);
        }
    }
    
    private void Update() {
        if (_catchAttemptTimer < _catchAttemptCooldown) {
            _catchAttemptTimer += Time.deltaTime;
        }
        
        if (_isCatching) {
            _windowCounter += Time.deltaTime;
        }
        
        if (_windowCounter >= _catchWindow) {
            _isCatching = false;
            _catchAttemptTimer = 0;
            _windowCounter = 0;
        }

        if (_hasBall.Value) {
            _throwTimer += Time.deltaTime;
            if (_throwTimer >= _throwTimeout)
                ThrowBallServerRpc();
        }
    }
    
    [ServerRpc]
    private void ThrowBallServerRpc()
    {
        if (!_hasBall.Value) return; // Only throw if the player has possession

        _hasBall.Value = false;
        _isCatching = false;
        GameObject ball = Instantiate(_ballPrefab, ItemSlot.position, Quaternion.identity);
        NetworkObject ballNetObj = ball.GetComponent<NetworkObject>();
        Ball ballRef = ball.GetComponent<Ball>();

        ballNetObj.Spawn();
        ballNetObj.ChangeOwnership(NetworkManager.ServerClientId); // Server takes control of ball

        ballRef.LaunchBall(ItemSlot.forward);
    }
    
    [ServerRpc]
    private void TryCatchServerRpc(ulong ballId, ServerRpcParams rpcParams = default)
    {
        // Server fetches the ball object
        NetworkObject ballNetObj = NetworkManager.SpawnManager.SpawnedObjects[ballId];
        if (ballNetObj == null) return;
        
        // Despawn the ball (player now owns it)
        ballNetObj.ChangeOwnership(rpcParams.Receive.SenderClientId);
        ballNetObj.Despawn();

        ReceiveBall();
    }
    
    public void ReceiveBall(ClientRpcParams clientRpcParams = default) {
        if (!IsOwner) return;
        
        Debug.Log("I got the ball");

        _throwTimer = 0;
        _hasBall.Value = true;
        GameManager.Instance.SetThrowerServerRpc(gameObject.GetComponent<PlayerIdentifier>().GetID); // Tell the GameManager that this player is now the Thrower
    }
}