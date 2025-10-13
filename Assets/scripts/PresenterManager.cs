using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PresenterManager : NetworkBehaviour
{
    public RawImage rawImage;
    public bool present = false;
    [SerializeField] int maxPresenters;
    ScreenReceiver currentPresenter;

    void Update()
    {
        if (present)
            rawImage.texture = currentPresenter.rawImage.texture;
        else
            rawImage.texture = Texture2D.blackTexture;
    }
    public void setPresenter(NetworkObjectReference networkObjectReference)
    {
        if (networkObjectReference.TryGet(out NetworkObject netObj))
        {
            currentPresenter = netObj.GetComponent<ScreenReceiver>();
            present = true;
            setPresentClientRpc(networkObjectReference);
        }
        else
        {
            Debug.LogWarning("Invalid NetworkObjectReference received!");
        }
    }
    [ClientRpc]
    public void setPresentClientRpc(NetworkObjectReference networkObjectReference)
    {
        if (networkObjectReference.TryGet(out NetworkObject netObj))
        {
            currentPresenter = netObj.GetComponent<ScreenReceiver>();
            present = true;
        }
        else
        {
            Debug.LogWarning("Invalid NetworkObjectReference received!");
        }
    }
    public void stopPresenting()
    {
        present = false;
        rawImage.texture = Texture2D.blackTexture;
        stopPresentingClientRpc();
    }
    [ClientRpc]
    public void stopPresentingClientRpc()
    {
        present = false;
        rawImage.texture = Texture2D.blackTexture;
    }
}
