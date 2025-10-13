using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PresenterManager : NetworkBehaviour
{
    public RawImage rawImage;
    public bool present = false;
    [SerializeField] int maxPresenters;
    ScreenReceiver currentPresenter;
    [SerializeField] TextMeshProUGUI presentingText;

    void Update()
    {
        if (present && currentPresenter != null)
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
    public void setPresent(bool isPresent)
    {
        present = isPresent;
        presentingText.text = present ? "Stop Presenting" : "Start Presenting";
        setPresentClientRpc(isPresent);
    }
    [ClientRpc]
    public void setPresentClientRpc(bool isPresent)
    {
        present = isPresent;
    }
}
