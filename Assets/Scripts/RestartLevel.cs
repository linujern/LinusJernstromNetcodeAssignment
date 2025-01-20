using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RestartLevel : MonoBehaviour {
    [SerializeField] private Button _restartButton;

    private void Start() {
        _restartButton.onClick.AddListener(RequestResetGame);
    }

    public void RequestResetGame() {
        if (NetworkManager.Singleton.IsServer) {
            ResetGame();
        }
        else {
            RequestResetGameServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestResetGameServerRpc() {
        ResetGame();
    }

    private void ResetGame() {
        if (!NetworkManager.Singleton.IsServer) return;

        // Reload the active scene for all clients
        NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }
    
    void Hide() => gameObject.SetActive(false);
}
