
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

public class SyncLocalGameTime : UdonSharpBehaviour
{
    // I want to synchronise the players so that they are on the same game time
    // Then I might be able to use this to enforce a sync state on players
    //
    // So to do this, I might have it so a player joins and presses a button to sync themself
    // What this then does is the instance owner will send their local time as a float and serialize this float
    [UdonSynced] private float localTime;
    [UdonSynced] private float localTime_TryingToSync;
    private float localTime_actuallyLocal;
    private float timeOffset;

    public TextMeshProUGUI syncStatusDebug;

    public void SyncLocalTime()
    {
        //localTime = Time.realtimeSinceStartup;
        //RequestSerialization();
    }

    private void FixedUpdate()
    {
        // Add to a local timer each Fixed Update.
        localTime_actuallyLocal += Time.fixedDeltaTime;

        // If we are the Owner of this Game Object (i.e. the Network Master)
        if (Networking.GetOwner(this.gameObject) == Networking.LocalPlayer)
        {
            // Once the local timer is equal to 1 or more
            if (localTime_actuallyLocal >= 1.0f)
            {
                // We are going to try and use two variables to calculate the sync timing
                // A timer that has tracks how long since we've last sent a serialization request
                localTime_TryingToSync = localTime_actuallyLocal;

                // A timer that tracks how long since start up
                localTime = Time.realtimeSinceStartup;

                //RequestSerialization();
                //OnDeserialization();

                // Reset the timer that tracks how long since last serialization request to 0, ONLY FOR THE INSTANCE MASTER
                localTime_actuallyLocal = 0.0f;
            }
        }
        else
        {

        }
        
    }

    void Start()
    {
        
    }

    public override void OnDeserialization()
    {
        // Then in here, we should be able to use the timer variables we serialized to sync both players

        syncStatusDebug.text = localTime.ToString() + "\n";
        syncStatusDebug.text += localTime_TryingToSync.ToString() + "\n";

        if (Networking.GetOwner(this.gameObject) != Networking.LocalPlayer)
        {
            syncStatusDebug.text += "\n";
            syncStatusDebug.text += localTime_actuallyLocal.ToString();
        }


        /*
        syncStatusDebug.text = (localTime + localTime_actuallyLocal).ToString();
        if (Networking.GetOwner(this.gameObject) != Networking.LocalPlayer)
        {
            syncStatusDebug.text += "\n";
            syncStatusDebug.text += localTime_actuallyLocal.ToString();
        }
        localTime_actuallyLocal = 0.0f;
        */
        //localTime_actuallyLocal = Time.realtimeSinceStartup;
        //timeOffset = localTime - localTime_actuallyLocal;
    }
}
