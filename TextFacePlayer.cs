
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TextFacePlayer : UdonSharpBehaviour
{
    RectTransform canvasTransform;
    VRCPlayerApi player;
    Canvas canvasCanvas;

    void Start()
    {
        canvasTransform = GetComponent<RectTransform>();
        player = Networking.LocalPlayer;
    }
    private void Update()
    {
        var headTrackingData = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

        canvasTransform.rotation = headTrackingData.rotation;

        //Debug.Log(canvasTransform.forward);
        //Debug.DrawRay(canvasTransform.position, canvasTransform.forward, Color.white, 0.5f);
    }
}
