
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class BatSwing : UdonSharpBehaviour
{
    // Big things that need to be done:
    //
    // Need get the point where the two rigidbodies collide
    // Because the angular and linear momentum are important (i think)
    //
    // Need to apply a force at this point
    // This could be an impulse rather than a force


    public GameObject BaseballBat;
    public GameObject Sandbag;

    private Rigidbody baseballBatRigidBody;


    void Start()
    {
        
    }

    /*
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("A collision has occured");

        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.DrawRay(contact.point, contact.normal, Color.red, 0.2f);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.DrawRay(contact.point, contact.normal, Color.red, 0.2f);
        }
    }
    */

    private void FixedUpdate()
    {
        
    }
}
