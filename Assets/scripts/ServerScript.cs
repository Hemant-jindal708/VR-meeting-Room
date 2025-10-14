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
    ButtonScript[] buttonScriptsKick = new ButtonScript[10];
    ButtonScript[] buttonScriptsPresenter = new ButtonScript[10];

    public void Start()
    {
        canvas.SetActive(true);
    }

    public void setpresenter(NetworkObject networkObject)
    {
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
        NetworkManager.Singleton.DisconnectClient(ClientID);
    }
    
    public void addButton(NetworkObjectReference networkObjectReference, string name)
    {
        if (networkObjectReference.TryGet(out NetworkObject networkObject))
        {

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
                kickbutton.interactable = true; // Ensure button is interactable
                kickbutton.onClick.RemoveAllListeners(); // Clear any existing listeners
                kickbutton.onClick.AddListener(() =>
                {
                    kickParticipant(clientId);
                });

                var kickText = kickbutton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (kickText != null)
                {
                    kickText.text = name;
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
                presenterbutton.interactable = true; // Ensure button is interactable
                presenterbutton.onClick.RemoveAllListeners(); // Clear any existing listeners
                presenterbutton.onClick.AddListener(() =>
                {
                    setpresenter(netObj);
                });

                var presenterText = presenterbutton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (presenterText != null)
                {
                    presenterText.text = name;
                }
            }

            kicklist.SetActive(iskicklistActive);
            presenterlist.SetActive(ispresenterlistActive);
            for (int i = 0; i < buttonScriptsKick.Length; i++)
            {
                if (buttonScriptsKick[i] == null)
                {
                    buttonScriptsKick[i] = kickbutton.GetComponent<ButtonScript>();
                    break;
                }
            }
            for (int i = 0; i < buttonScriptsPresenter.Length; i++)
            {
                if (buttonScriptsPresenter[i] == null)
                {
                    buttonScriptsPresenter[i] = presenterbutton.GetComponent<ButtonScript>();
                    break;
                }
            }
        }
    }
}