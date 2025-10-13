using TMPro;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class ButtonScript : MonoBehaviour
{
    public void setpresenter(NetworkObject networkObject)
    {
        var presenterMgr = FindAnyObjectByType<PresenterManager>();
        if (presenterMgr != null)
            presenterMgr.setPresenter(new NetworkObjectReference(networkObject));
        else
            Debug.LogWarning("PresenterManager not found!");
    }
    public void kickParticipant(ulong clientId)
    {
        Debug.Log($"Kicking participant {clientId}");
        NetworkManager.Singleton.DisconnectClient(clientId);
    }
    public void setPresenterEvent(NetworkObject networkObject)
    {
        GetComponent<Button>().onClick.AddListener(() => setpresenter(networkObject));
    }
    public void kickParticipantEvent(ulong clientId)
    {
        GetComponent<Button>().onClick.AddListener(() => kickParticipant(clientId));
    }
}
