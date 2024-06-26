
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class BatPartFollower : UdonSharpBehaviour
{

    // OK so basically what do I want to do?
    // I want to 'attach' this gameObject, which contains a collider and rigidbody, to the bat
    // I cannot put this part as a child to the bat because when the bat is picked up, all of the built in unity functions that calculate velocity break
    //
    // So to do this I will need to get the transform of the bat and use this to calculate the position of this BatPartFollower gameObject

    public GameObject bat;

    private Transform batTransform;
    private Transform batFollowerTransform;

    void Start()
    {
        batTransform = bat.GetComponent<Transform>();
        batFollowerTransform = this.GetComponent<Transform>();
    }

    private void FixedUpdate()
    {

        batFollowerTransform.position = batTransform.position;
        batFollowerTransform.rotation = batTransform.rotation;
    }
}
