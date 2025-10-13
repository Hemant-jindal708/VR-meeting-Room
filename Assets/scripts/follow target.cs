using UnityEngine;

public class followtarget : MonoBehaviour
{
    Transform target;
    [SerializeField] GameObject triverse;
    public Vector3 offset;

    void Start()
    {
        Debug.Log("Finding target...");
        target = FindChildRecursive(triverse.transform, "Head").transform;
        Debug.Log(target.name);
        
    }
    void Update()
    {
        transform.position = target.position + offset;
    }
    static public GameObject FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child.gameObject;
            GameObject result = FindChildRecursive(child, childName);
            if (result != null)
                return result;
        }
        return null;
    }
}
