using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

public class VRScript : NetworkBehaviour
{
    public GameObject camera1;
    Quaternion initialRotation;

    void Start()
    {
        if (!IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }
        initialRotation = DeviceRotation.Get();
        Input.gyro.enabled = true;
    }

    void Update()
    {
        if (!IsOwner) return;

        Quaternion devicerotation = DeviceRotation.Get();
        // calibrate the gyro inputs
        devicerotation = Quaternion.Euler(devicerotation.eulerAngles - initialRotation.eulerAngles);
        // Debug.Log("Device Rotation: " + devicerotation.eulerAngles);

        camera1.transform.localRotation = Quaternion.Euler(devicerotation.eulerAngles.x, devicerotation.eulerAngles.y, 0);

        SendRotationToServerRpc(devicerotation);
    }

    [ServerRpc]
    void SendRotationToServerRpc(Quaternion rotation)
    {
        camera1.transform.localRotation = Quaternion.Euler(rotation.eulerAngles.x, rotation.eulerAngles.y, 0);
        UpdateRotationClientRpc(rotation);
    }

    [ClientRpc]
    void UpdateRotationClientRpc(Quaternion rotation)
    {
        if (IsOwner) return;

        camera1.transform.localRotation = Quaternion.Euler(rotation.eulerAngles.x, rotation.eulerAngles.y, 0);
    }
}
