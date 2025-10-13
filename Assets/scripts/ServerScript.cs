using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ServerScript : NetworkBehaviour
{
    [SerializeField] GameObject canvas;
    [SerializeField] GameObject buttonPrefab;
    [SerializeField] GameObject kicklist;
    [SerializeField] GameObject presenterlist;
    ButtonScript[] buttonScriptsKick;
    ButtonScript[] buttonScriptsPresenter;

    public void Start()
    {
        Debug.Log("Server started");
        canvas.SetActive(true);
    }

    public void setpresenter(NetworkObject networkObject)
    {
        Debug.Log($"[Server] setpresenter called for client {networkObject.OwnerClientId}");
        FindAnyObjectByType<PresenterManager>().setPresenter(new NetworkObjectReference(networkObject));
    }

    public void kickParticipant(ulong ClientID)
    {
        foreach (var btn in buttonScriptsKick)
        {
            if (btn != null && btn.clientId == ClientID)
            {
                Destroy(btn.gameObject);
                break;
            }
        }
        foreach (var btn in buttonScriptsPresenter)
        {
            if (btn != null && btn.clientId == ClientID)
            {
                Destroy(btn.gameObject);
                break;
            }
        }
        Debug.Log($"[Server] kickParticipant called for client {ClientID}");
        NetworkManager.Singleton.DisconnectClient(ClientID);
    }

    public void addButton(NetworkObjectReference networkObjectReference, string name)
    {
        Debug.Log($"[Server] Adding button for client {name}");
        if (networkObjectReference.TryGet(out NetworkObject networkObject))
        {
            Debug.Log($"[Server] Successfully retrieved NetworkObject for client {name}");

            // Store values before any operations
            ulong clientId = networkObject.OwnerClientId;
            NetworkObject netObj = networkObject;

            bool iskicklistActive = kicklist.activeSelf;
            bool ispresenterlistActive = presenterlist.activeSelf;

            kicklist.SetActive(true);
            presenterlist.SetActive(true);

            // Kick button
            GameObject kickbuttonprefab = Instantiate(buttonPrefab, kicklist.transform);
            Button kickbutton = kickbuttonprefab.GetComponent<Button>();
            kickbutton.GetComponent<ButtonScript>().clientId = clientId;
            if (kickbutton == null)
            {
                Debug.LogError("[Server] Kick button component is null!");
            }
            else
            {
                Debug.Log($"[Server] Kick button created successfully");
                kickbutton.interactable = true; // Ensure button is interactable
                kickbutton.onClick.RemoveAllListeners(); // Clear any existing listeners
                kickbutton.onClick.AddListener(() =>
                {
                    Debug.Log($"[Server] Kick button clicked for client {clientId}");
                    kickParticipant(clientId);
                });

                var kickText = kickbutton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (kickText != null)
                {
                    kickText.text = name;
                    Debug.Log($"[Server] Kick button text set to {name}");
                }
                else
                {
                    Debug.LogError("[Server] TextMeshProUGUI component not found on kick button!");
                }
            }

            // Presenter button
            GameObject presenterbuttonprefab = Instantiate(buttonPrefab, presenterlist.transform);
            Button presenterbutton = presenterbuttonprefab.GetComponent<Button>();
            presenterbutton.GetComponent<ButtonScript>().clientId = clientId;
            if (presenterbutton == null)
            {
                Debug.LogError("[Server] Presenter button component is null!");
            }
            else
            {
                Debug.Log($"[Server] Presenter button created successfully");
                presenterbutton.interactable = true; // Ensure button is interactable
                presenterbutton.onClick.RemoveAllListeners(); // Clear any existing listeners
                presenterbutton.onClick.AddListener(() =>
                {
                    Debug.Log($"[Server] Presenter button clicked for client {clientId}");
                    setpresenter(netObj);
                });

                var presenterText = presenterbutton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (presenterText != null)
                {
                    presenterText.text = name;
                    Debug.Log($"[Server] Presenter button text set to {name}");
                }
                else
                {
                    Debug.LogError("[Server] TextMeshProUGUI component not found on presenter button!");
                }
            }

            kicklist.SetActive(iskicklistActive);
            presenterlist.SetActive(ispresenterlistActive);
            buttonScriptsKick.Append(kickbuttonprefab.GetComponent<ButtonScript>());
            buttonScriptsPresenter.Append(presenterbuttonprefab.GetComponent<ButtonScript>());
            Debug.Log($"[Server] Buttons added for {name}. Kick list active: {iskicklistActive}, Presenter list active: {ispresenterlistActive}");
        }
        else
        {
            Debug.LogError($"[Server] Failed to retrieve NetworkObject for {name}");
        }
    }
}