using UnityEngine;
using ReadyPlayerMe.Core;
using Unity.Netcode;

public class ServerAvatarLoader : NetworkBehaviour
{
    [Header("Network Prefab Container (registered in NetworkManager)")]
    [SerializeField] private GameObject avatarContainerPrefab;

    [Header("Animator Controller")]
    public RuntimeAnimatorController animatorController;

    private AvatarObjectLoader avatarLoader;
    private Transform parentTransform;
    private GameObject currentAvatar;

    private void Awake()
    {
        avatarLoader = new AvatarObjectLoader();
        avatarLoader.OnCompleted += OnAvatarLoaded;
        avatarLoader.OnFailed += OnAvatarLoadFailed;
    }

    [ServerRpc(RequireOwnership = false)]
    public void LoadAvatarServerRpc(string url, NetworkObjectReference parentReference)
    {
        if (parentReference.TryGet(out NetworkObject netObj))
        {
            parentTransform = netObj.transform;
            avatarLoader.LoadAvatar(url);
            LoadAvatarClientRpc(url, parentReference);
        }
    }
    [ClientRpc]
    public void LoadAvatarClientRpc(string url, NetworkObjectReference parentReference)
    {
        if (parentReference.TryGet(out NetworkObject netObj))
        {
            parentTransform = netObj.transform;
            avatarLoader.LoadAvatar(url);
        }
    }

    private void OnAvatarLoaded(object sender, CompletionEventArgs args)
    {
        currentAvatar = args.Avatar;

        // Spawn the networked container (this prefab is registered in NetworkManager)
        GameObject container = Instantiate(avatarContainerPrefab, parentTransform.position, parentTransform.rotation);
        // Parent the ReadyPlayerMe avatar under the container
        currentAvatar.transform.SetParent(container.transform, false);

        // Apply animator controller
        Animator animator = currentAvatar.GetComponent<Animator>();
        if (animator != null && animatorController != null)
        {
            animator.runtimeAnimatorController = animatorController;
        }

        // Attach look-at constraint if needed
        Transform armature = currentAvatar.transform.Find("Armature");
        if (armature != null)
        {
            Transform head = FindChildRecursive(armature, "Head");
            Transform target = FindChildRecursive(parentTransform, "target");
            if (head != null && target != null)
            {
                LookAtConstraint lookAt = head.gameObject.AddComponent<LookAtConstraint>();
                lookAt.target = target;
                lookAt.weight = 1f;
            }
        }

        // Sync transform with parent
        container.transform.SetParent(parentTransform);
        container.transform.localPosition = Vector3.zero;
        container.transform.localRotation = Quaternion.identity;

        Debug.Log("[ServerAvatarLoader] Avatar container spawned and parented successfully.");
    }

    private void OnAvatarLoadFailed(object sender, FailureEventArgs args)
    {
        Debug.LogError($"[ServerAvatarLoader] Failed to load avatar: {args.Message}");
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(childName)) return child;
            var result = FindChildRecursive(child, childName);
            if (result != null) return result;
        }
        return null;
    }
}

public class LookAtConstraint : MonoBehaviour
{
    public Transform target;
    [Range(0, 1f)] public float weight = 1f;
    public Vector3 rotationOffset;

    private void LateUpdate()
    {
        if (target == null || weight <= 0f) return;

        Quaternion targetRotation = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation * Quaternion.Euler(rotationOffset),
            weight
        );
    }
}

