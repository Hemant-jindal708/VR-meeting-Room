using UnityEngine;

public class CameraChanger : MonoBehaviour
{
    [SerializeField] GameObject CameraL;
    [SerializeField] GameObject CameraR;
    [SerializeField] GameObject CameraM;
    public bool VRActive = true;
    VRScript vrScript;
    void Start()
    {
        vrScript = GetComponent<VRScript>();
        CameraL.SetActive(false);
        CameraR.SetActive(false);
        CameraM.GetComponent<Camera>().enabled=false;
        vrScript.enabled = false;
        if (VRActive)
        {
            CameraL.SetActive(true);
            CameraR.SetActive(true);
            vrScript.enabled = true;
        }
        else
        {
            CameraM.GetComponent<Camera>().enabled=true;
        }
    }
}