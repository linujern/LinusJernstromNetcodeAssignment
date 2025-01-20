using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.Netcode;

public class GameManager : NetworkBehaviour {
    [SerializeField] private GameObject _ballPrefab;
    [SerializeField] private Canvas _gameOverCanvas;
    [SerializeField] private TMP_Text _gameOverText;

    #region Singleton
    public static GameManager Instance { get; private set; }

    private void Awake() {
        if (Instance == null)
            Instance = this;
        else 
            DestroyImmediate(this);
    }
    #endregion
    
    private NetworkVariable<int> _throwerInstanceID = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private List<int> _playerIDs = new List<int>();
    public int GetThrower() => _throwerInstanceID.Value;
    
    [ServerRpc]
    public void SetThrowerServerRpc(int playerId) {
        _throwerInstanceID.Value = playerId;
    }

    public void AddUniquePlayerID(int ID) {
        if(_playerIDs.Contains(ID)) return;
        _playerIDs.Add(ID);
    }

    public void RemovePlayerID(int ID) {
        if (_playerIDs.Contains(ID))
            _playerIDs.Remove(ID);
        
        if (_playerIDs.Count <= 1)
            GameOverServerRpc();
    }

    [ServerRpc]
    private void GameOverServerRpc() {
        if (!IsOwner) return;
        
        if(_playerIDs.Count == 0) 
            GameOverClientRpc(9999);
        
        int winnerID = _playerIDs[0];
        GameOverClientRpc(winnerID);
    }
    
    [ClientRpc]
    private void GameOverClientRpc(int winnerID)
    {
        if (!IsOwner) return; // Ensure only the local player's UI is affected
        
        _gameOverCanvas.gameObject.SetActive(true);
        _gameOverText.SetText(NetworkManager.LocalClient.PlayerObject?.GetInstanceID() == winnerID ? "YOU WON!" : "YOU LOST!");
    }
    
    public override void OnNetworkSpawn() {
        if (IsServer) {
            SpawnBall();
        }
    }

    private void SpawnBall() {
        GameObject ball = Instantiate(_ballPrefab, new Vector3(5f, .5f, 0f), Quaternion.identity);
        ball.GetComponent<NetworkObject>().Spawn();
    }
}

