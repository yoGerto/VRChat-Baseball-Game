
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using Unity.Mathematics;

public class PlayerCamera : UdonSharpBehaviour
{
    private Camera cameraTest;
    private Transform cameraTest_Transform;
    private VRCPlayerApi player;
    private bool camActive = false;

    VRCPlayerApi.TrackingData headTrackingData;
    Quaternion headRot;
    Vector3 cameraHeightOffset = Vector3.zero;

    public TextMeshProUGUI debugText;

    void Start()
    {
        cameraTest = this.GetComponent<Camera>();
        cameraTest_Transform = this.transform;
        player = Networking.LocalPlayer;
    }

    public void LockCam()
    {
        if (!camActive)
        {
            // Populate the headTrackingData with the head's pos and rotation
            headTrackingData = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

            // Store the initial rotation in a variable for later as we want to return the player to this rotation later
            headRot = headTrackingData.rotation;

            // Set the camera Transform to the position and rotation at the moment OnPickupUseDown is called
            cameraTest_Transform.SetLocalPositionAndRotation(headTrackingData.position, headRot);

            // Store how far the camera is off of the ground when this is called
            cameraHeightOffset = headTrackingData.position - player.GetPosition();

            cameraTest.enabled = true;
            camActive = true;
        }
        else
        {
            
            Vector3 playerPos = player.GetPosition();

            // Teleport the player to their current position, with the rotation which should be what at the start 
            //player.TeleportTo(playerPos, headRot);
            player.TeleportTo(playerPos, new Quaternion(0.0f, 0.0f, 0.0f, 0.0f), VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint);
            //player.TeleportTo(playerPos, glorp.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint);

            cameraTest.enabled = false;
            camActive = false;
        }     
    }

    public void Update()
    {

        headTrackingData = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        debugText.text = "stored headRot = " + headRot.ToString() + "\n";
        debugText.text += "current headRot = " + headTrackingData.rotation.ToString();
        /*
        if (camActive)
        {
            headTrackingData = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            //Vector3 playerPos = player.GetPosition();
            cameraTest_Transform.SetLocalPositionAndRotation(headTrackingData.position, headRot);
        }
        */
    }
}
