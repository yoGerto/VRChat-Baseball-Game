
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using VRC.Udon.Common;

public class SyncLocalGameTime : UdonSharpBehaviour
{
    [UdonSynced] private float localTime;
    private float localTime_actuallyLocal;
    private float timeOffset = 0.0f;

    public Sandbag sandbag;

    public void SyncLocalTime()
    {
        if (Networking.IsMaster)
        {
            return;
        }
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SyncLocalTime_Networked));
    }

    public void SyncLocalTime_Networked()
    {
        localTime = Time.realtimeSinceStartup;
        RequestSerialization();
    }


    public override void OnDeserialization(DeserializationResult dr)
    {
        timeOffset = (localTime + (Time.realtimeSinceStartup - dr.sendTime)) - Time.realtimeSinceStartup;
        sandbag.SetProgramVariable("timeOffset", timeOffset);
    }
}