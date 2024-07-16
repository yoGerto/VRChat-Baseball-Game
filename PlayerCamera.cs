
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
                    // Toggle Third Person
                    thirdPersonMode = !thirdPersonMode;
                    //camActive = !camActive;
                    //cameraTest.enabled = camActive;
                }
                else
                {

                }
                startCounting = false;
                timer = 0.0f;
            }
        }

        if (startCounting)
        {
            timer += Time.deltaTime;
            if (timer > 0.5f)
            {
                // Lock Camera In Place
            }
        }

        if (thirdPersonMode)
        {
            Vector3 posDiff = bat.transform.position - sandbag.transform.position;
            Debug.DrawRay(sandbag.transform.position, posDiff, Color.yellow, 1.0f);
            cameraTest.transform.position = posDiff;
            // calculate difference between bat pos and sandbag pos
            // then normalize this difference to get a vector of length 1, and then add this to the difference to get a third person pov
        }

        buttonHeldPrevious = buttonHeld;
    }
}
