using Unity.Netcode;

public class PlayerIdentifier : NetworkBehaviour
{
    private int _playerID;
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (!IsOwner) return;

        _playerID = gameObject.GetInstanceID();
        GameManager.Instance.AddUniquePlayerID(_playerID);
    }

    public int GetID => _playerID;
}
