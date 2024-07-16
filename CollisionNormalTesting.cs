
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CollisionNormalTesting : UdonSharpBehaviour
{
    public GameObject testGameObject;


    private void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (collision.collider.name == "Cube")
            {
                Debug.DrawRay(contact.normal, contact.normal, Color.blue, 1.0f);
                Debug.DrawLine(contact.normal, new Vector3(0, 0, 0), Color.red, 1.0f);
                Debug.Log(contact.normal);
                testGameObject.transform.position = contact.normal;
            }
        }
    }
    void Start()
    {
        
    }
}
