using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ChairPositions : MonoBehaviour
{
    [SerializeField] GameObject[] chair;
    [SerializeField] List<Chair> chairs = new List<Chair>();
    void Awake()
    {
        foreach (GameObject c in chair)
        {
            chairs.Add(new Chair(c.transform));
        }
    }
    public Transform getChair(GameObject partcipent)
    {
        foreach (Chair c in chairs)
        {
            if (c.participent == null)
            {
                c.participent = partcipent;
                return c.position;
            }
        }
        return null;
    }
}
class Chair
{
    public Transform position;
    public GameObject participent;
    public Chair(Transform pos)
    {
        position = pos;
    }
}