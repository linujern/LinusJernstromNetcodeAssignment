using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkStartUI : MonoBehaviour
{
    [SerializeField] private Button _startHostButton;
    [SerializeField] private Button _startClientButton;

    private void Start() {
        _startHostButton.onClick.AddListener(StartHost);
        _startClientButton.onClick.AddListener(StartClient);
    }

    void StartHost() {
        Debug.Log("Starting Host");
        NetworkManager.Singleton.StartHost();
        Hide();
    }

    void StartClient() {
        Debug.Log("Starting Client");
        NetworkManager.Singleton.StartClient();
        Hide();
    }

    void Hide() => gameObject.SetActive(false);
}
