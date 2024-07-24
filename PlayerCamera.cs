
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using Unity.Mathematics;

public class PlayerCamera : UdonSharpBehaviour
{
    public GameObject sandbag;
    public GameObject bat;

    private Camera cameraTest;
    private VRCPlayerApi player;
    private bool camActive = false;
    private bool buttonHeld = false;
    private bool buttonHeldPrevious = false;
    private bool lockCam = false;
    private bool startCounting = false;
    private bool thirdPersonMode = false;
    private float timer = 0.0f;

    VRCPlayerApi.TrackingData headTrackingData;
    Quaternion headRot;
    Vector3 cameraHeightOffset = Vector3.zero;

    public TextMeshProUGUI debugText;

    void Start()
    {
        cameraTest = this.GetComponent<Camera>();
        player = Networking.LocalPlayer;
    }

    public void LockCam()
    {
        // When OnPickupUseDown is sent, buttonHeld becomes true
        // Then when OnPickupUseUp is sent, buttonHeld becomes false
        buttonHeld = !buttonHeld;
    }

    public void BatDropped()
    {
        thirdPersonMode = false;
        camActive = false;
        cameraTest.enabled = camActive;
    }

    public void Update()
    {
        if (buttonHeld != buttonHeldPrevious)
        {
            if (buttonHeld)
            {
                startCounting = true;
            }
            else
            {
                if (timer < 0.5f)
                {
                    lockCam = false;
                    // Toggle Third Person
                    thirdPersonMode = !thirdPersonMode;
                    camActive = !camActive;
                    cameraTest.enabled = camActive;
                }
                else
                {
                    lockCam = !lockCam;
                }
                startCounting = false;
                timer = 0.0f;
            }
        }

        if (startCounting)
        {
            timer += Time.deltaTime;
            if (timer > 0.3f)
            {
                // Lock Camera In Place
            }
        }

        if (thirdPersonMode)
        {
            // If !lockCam, update camera pos every cycle
            if (!lockCam)
            {
                Vector3 posDiff = bat.transform.position - sandbag.transform.position;
                Debug.DrawRay(sandbag.transform.position, posDiff, Color.yellow, 1.0f);
                cameraTest.transform.position = bat.transform.position + (posDiff.normalized * 3);
                cameraTest.transform.LookAt(sandbag.transform.position);
            }
            // Then when lock cam is switched to true, the camera will remain in it's last position every frame

        }

        buttonHeldPrevious = buttonHeld;
    }
}
