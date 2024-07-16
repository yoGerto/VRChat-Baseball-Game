
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class BatFollower : UdonSharpBehaviour
{
    public GameObject batToFollow;
    public bool lockBat = false;

    void Start()
    {
        
    }

    private void Update()
    {
        if (!lockBat)
        {
            this.transform.position = batToFollow.transform.position;
            this.transform.rotation = batToFollow.transform.rotation;
        }
    }
}
