using UnityEngine;

public class SelectAvatar : MonoBehaviour
{
    public int selectedIndex = 0;
    [SerializeField] GameObject[] avatars;
    ButtonBehaviourAdder buttonBehaviourAdder;
    void Awake()
    {
        buttonBehaviourAdder = FindAnyObjectByType<ButtonBehaviourAdder>();
    }
    public void NextAvatar()
    {
        avatars[selectedIndex].SetActive(false);
        selectedIndex = (selectedIndex + 1) % avatars.Length;
        avatars[selectedIndex].SetActive(true);
        buttonBehaviourAdder.avatarIndex = selectedIndex;
    }
    public void PreviousAvatar()
    {
        avatars[selectedIndex].SetActive(false);
        selectedIndex = (selectedIndex - 1 + avatars.Length) % avatars.Length;
        avatars[selectedIndex].SetActive(true);
        buttonBehaviourAdder.avatarIndex = selectedIndex;
    }
}
