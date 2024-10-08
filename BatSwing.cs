﻿
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class BatSwing : UdonSharpBehaviour
{
    // A couple things that I think should be done within this script
    //
    // The first is that, we need to track what players have already swung at the sandbag
    // My thought is that we can have a max number of swings that can happen on the sandbag or we can allow each player to take 1 swing
    // So we need to keep a tally of either the number of swings or the players who have swung
    //
    // The second thing to do, which could be done in this script or else where, is to lock the camera's position and rotation when a desktop user is swinging
    // This can be done by checking when they are doing OnPickupUseDown, which is when the player left click's whilst holding the object.

    public GameObject[] batParts;
    public GameObject BaseballBat;
    public GameObject Sandbag;
    public TextMeshProUGUI syncStatusDebug;
    public PlayerCamera playerCamera;

    private bool hasSwung = false;

    Sandbag sandbagScript;
    Rigidbody batRB;
    float timer = 0.0f;
    bool freezeTimer = false;

    void Start()
    {
        sandbagScript = Sandbag.GetComponent<Sandbag>();
        batRB = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // If the Local player does not own the bat, do nothing and exit
        if (Networking.GetOwner(this.gameObject) != Networking.LocalPlayer)
        {
            return;
        }

        // If the collision is with the Floor or Big Floor, do nothing and exit
        if (collision.collider.name == "Floor")
        {
            return;
        }
        if (collision.collider.name == "Big Floor Test")
        {
            return;
        }


        // If we are here that means that the collision is between a BatPart and the Sandbag
        // So we want to keep a record of that.
        hasSwung = true;

        // We might also want to make the bat owner (local player) the owner of the Sandbag. This is to test to see if handing the ownership is viable like this
        Networking.SetOwner(Networking.LocalPlayer, Sandbag);
    }

    
    public override void OnPickup()
    {
        sandbagScript.SetProgramVariable("isBatHeld", true);
    }

    public override void OnDrop()
    {
        sandbagScript.SetProgramVariable("isBatHeld", false);
        playerCamera.BatDropped();
    }

    public override void OnPickupUseDown()
    {
        playerCamera.LockCam();
    }

    public override void OnPickupUseUp()
    {
        playerCamera.LockCam();
    }

    public void ClearSwing()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ClearSwing_Networked));
    }

    public void ClearSwing_Networked()
    {
        hasSwung = false;
    }

    private void Update()
    {
        if (freezeTimer)
        {
            timer += Time.deltaTime;
            if (timer > 0.25f)
            {
                timer = 0.0f;
                freezeTimer = false;
                batRB.constraints = RigidbodyConstraints.None;
            }
        }
    }
}
