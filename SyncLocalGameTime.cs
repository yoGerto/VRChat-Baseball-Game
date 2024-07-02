
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using VRC.Udon.Common;

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
    private float timeOffset = 0.0f;
    private int test = 0;

    public TextMeshProUGUI syncStatusDebug;
    public TextMeshProUGUI syncStatusDebug2;

    public void SyncLocalTime()
    {
        Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        localTime = Time.realtimeSinceStartup;
        RequestSerialization();
        //OnDeserialization();
    }

    private void FixedUpdate()
    {
        syncStatusDebug2.text = (Time.realtimeSinceStartup + timeOffset).ToString();
    }

    void Start()
    {
        
    }

    public override void OnDeserialization(DeserializationResult dr)
    {
        syncStatusDebug.text = "time sent by host = " + localTime + "\n";
        syncStatusDebug.text += "time on local client side = " + Time.realtimeSinceStartup + "\n";
        syncStatusDebug.text += "dr.sendTime = " + dr.sendTime + "\n";
        syncStatusDebug.text += "dr.receiveTime = " + dr.receiveTime + "\n";

        syncStatusDebug.text += "\n";
        syncStatusDebug.text += "delta between send and realtime = " + (Time.realtimeSinceStartup - dr.sendTime) + "\n";
        syncStatusDebug.text += "delta between host time and client time = " + (localTime + (Time.realtimeSinceStartup - dr.sendTime)).ToString();

        timeOffset = (localTime + (Time.realtimeSinceStartup - dr.sendTime)) - Time.realtimeSinceStartup;

        /*
        syncStatusDebug.text = "test " + test.ToString();
        test += 1;
        */
    }
}
/*
syncStatusDebug.text = localTime.ToString() + "\n";
syncStatusDebug.text += deserializationResult.sendTime.ToString() + "\n";
syncStatusDebug.text += deserializationResult.receiveTime.ToString() + "\n";
syncStatusDebug.text += "Serialization was sent " + (Time.realtimeSinceStartup - deserializationResult.sendTime).ToString() + " seconds ago" + "\n";
*/