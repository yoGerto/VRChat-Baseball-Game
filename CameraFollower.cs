
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CameraFollower : UdonSharpBehaviour
{
    public GameObject sandbag;
    public GameObject cameraFollower;

    private Transform sandbagTransform;
    private Transform cameraFollowerTransform;

    private Vector3 followerOffset;
    private Quaternion cameraRotation;

    void Start()
    {
        cameraFollower = this.gameObject;
        
        sandbagTransform = sandbag.GetComponent<Transform>();
        cameraFollowerTransform = cameraFollower.GetComponent<Transform>();

        followerOffset = sandbagTransform.position - cameraFollowerTransform.position;
        cameraRotation = cameraFollowerTransform.rotation;
    }

    private void Update()
    {
        cameraFollowerTransform.position = sandbagTransform.position - followerOffset;
        cameraFollowerTransform.rotation = cameraRotation;
    }
}
