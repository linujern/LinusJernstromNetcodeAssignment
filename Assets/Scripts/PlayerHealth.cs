using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour {
    [SerializeField] private TMP_Text _healthText;
    [SerializeField] private bool _isInvulnerable = false;
    private NetworkVariable<int> _health = new NetworkVariable<int>(3);
    private PlayerIdentifier _playerIdentifier;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        _healthText?.SetText(_health.Value.ToString());
        _health.OnValueChanged += (oldValue, newValue) => UpdateHealthUI(newValue);
        _playerIdentifier = GetComponent<PlayerIdentifier>();
    }
    
    public void DealDamage() {
        TakeDamage();
    }
    
    private void TakeDamage() {
        if (!_isInvulnerable) 
            _health.Value--;
        
        if (!IsAlive()) 
            DeathClientRpc();
    }

    private void UpdateHealthUI(int newHealth) {
        _healthText?.SetText(newHealth.ToString());
    }

    public int GetHealth() => _health.Value;

    public bool IsAlive() => _health.Value > 0;

    [ClientRpc]
    private void DeathClientRpc() {
        GameManager.Instance.RemovePlayerID(_playerIdentifier.GetID);
    }
}
