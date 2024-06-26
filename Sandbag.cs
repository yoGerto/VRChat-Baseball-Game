
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

public class Sandbag : UdonSharpBehaviour
{
    public GameObject[] batParts;
    public TextMeshProUGUI rotationText;

    private Vector3 batVelocity = Vector3.zero;
    private Vector3 batPosCurr = Vector3.zero;
    private Vector3 batPosPrev = Vector3.zero;

    private Quaternion batRotationCurr = Quaternion.identity;
    private Quaternion batRotationPrev = Quaternion.identity;
    private Quaternion batRotationDelta = Quaternion.identity;

    float linearMomentum;

    //private float batRotationYCurr = 0.0f;
    //private float batRotationYPrev = 0.0f;
    //private float batRotationYDiff = 0.0f;

    private float m_ass = 1.4f; //mass in kg
    // I know this isnt proper naming convention but it says ass lol
    // Average baseball bat weighs between 0.96kg to 1.4kg
    private float sandbagWeight = 20.0f;

    private float timer = 0.0f;
    private float cooldownTime = 0.5f;
    private bool timerLatch = false;

    private void OnCollisionEnter(Collision collision)
    {

        foreach (ContactPoint contact in collision.contacts)
        {
            if (collision.collider.name == "BatPart (0)")
            {
                // Get the contact normal and multiply it by the linear momentum vector to determine the resultant impulse
                rotationText.text = "batVelocity = " + batVelocity.ToString() + "\n";
                rotationText.text += "contact.normal = " + contact.normal.ToString() + "\n";

                //Vector3 resultantImpulse = new Vector3(Mathf.Abs(contact.normal.x) * batVelocity.x, Mathf.Abs(contact.normal.y) * batVelocity.y, Mathf.Abs(contact.normal.z) * batVelocity.z);
                //I need to find the angle between the normal and the velocity to use that to determine the contribution

                float triangleSideA = Mathf.Sqrt( (contact.normal.x * contact.normal.x) + (contact.normal.z * contact.normal.z) ); // This is the absolute distance of the contact normal, only considering 2D for now
                float triangleSideB = Mathf.Sqrt( (batVelocity.x * batVelocity.x) + (batVelocity.z * batVelocity.z)); // This is the absolute distance of the velocity vector, again only 2D for now
                float triangleSideC = Mathf.Sqrt((Mathf.Pow((batVelocity.x - contact.normal.x), 2)) + (Mathf.Pow((batVelocity.z - contact.normal.z), 2))); // This is the difference between the two Vectors

                float topOfAngleEquation = (triangleSideA * triangleSideA) + (triangleSideB * triangleSideB) - (triangleSideC * triangleSideC);
                float bottomOfAngleEquation = (2 * triangleSideA * triangleSideB);

                float angleBetweenNormalAndVelocity = Mathf.Acos(topOfAngleEquation / bottomOfAngleEquation);
                //Debug.Log("angle = " + angleBetweenNormalAndVelocity * (180/Mathf.PI));

                Vector3 resultantImpulse = contact.normal * (triangleSideB * Mathf.Cos(angleBetweenNormalAndVelocity));


                rotationText.text += "resultantImpulse = " + resultantImpulse.ToString();

                //Debug.Log("batVelocity = " + batVelocity.ToString());
                //Debug.Log("contact.normal = " + contact.normal.ToString());

                Debug.DrawRay(contact.point, batVelocity, Color.green, 1.0f);
                Debug.DrawRay(contact.point, resultantImpulse, Color.blue, 1.0f);
                Debug.DrawRay(contact.point, contact.normal, Color.red, 1.0f);

                //Vector3 resultantImpulse = new Vector3(contact.normal.y * batVelocity.x, contact.normal.x * batVelocity.y, contact.normal.z * batVelocity.z);
                //rotationText.text = linearMomentum.ToString();
                //Debug.DrawRay(contact.point, resultantImpulse, Color.green, 1.0f);
            }
            //Debug.Log(collision.collider.name);
            //Debug.DrawRay(contact.point, contact.normal, Color.red, 1.0f);
            //Debug.Log(contact.normal);
        }
        if (collision.collider.name == "BatPart (0)")
        {
            //rotationText.text = linearMomentum.ToString();
        }
    }

    void Start()
    {

    }

    private void FixedUpdate()
    {
        //rotationText.text = bat.transform.position.ToString() + "\n" + "\n";
        //rotationText.text += bat.transform.rotation.ToString() + "\n" + "\n";
        
        batPosCurr = batParts[0].transform.position;
        batVelocity = (batPosCurr - batPosPrev) / Time.fixedDeltaTime;
        batPosPrev = batPosCurr;

        linearMomentum = batVelocity.magnitude * m_ass;

        float angleOut;
        Vector3 axisOut;

        batRotationCurr = batParts[0].transform.rotation;
        batRotationDelta = batRotationCurr * Quaternion.Inverse(batRotationPrev);
        batRotationPrev = batRotationCurr;

        batRotationDelta.ToAngleAxis(out angleOut, out axisOut);

        angleOut *= Mathf.Deg2Rad;

        Vector3 angularVelocity = (angleOut * axisOut) / Time.fixedDeltaTime;
    }
}

/*
//rotationText.text = bat.transform.position.ToString() + "\n" + "\n";
//rotationText.text += bat.transform.rotation.ToString() + "\n" + "\n";

// Essentially two things to calculate
// The Linear Momentum and the Angular Momentum
// Linear momentum is Mass * Velocity

//float linearMomentum;

batPosCurr = batParts[0].transform.position;
batVelocity = (batPosCurr - batPosPrev) / Time.fixedDeltaTime;
batPosPrev = batPosCurr;

//rotationText.text += "Velocity = " + batVelocity.magnitude.ToString("0.00") + "m/s" + "\n" + "\n";

linearMomentum = batVelocity.magnitude * m_ass;

//rotationText.text += "Momentum = " + linearMomentum.ToString("0.00") + "kgm/s" + "\n" + "\n";

//float angularVelocity;

//batRotationYCurr = bat.transform.rotation;
//batRotationYDiff = batRotationYCurr - batRotationYPrev;
//batRotationYPrev = batRotationYCurr;

float angleOut;
Vector3 axisOut;

batRotationCurr = batParts[0].transform.rotation;
batRotationDelta = batRotationCurr * Quaternion.Inverse(batRotationPrev);
batRotationPrev = batRotationCurr;

batRotationDelta.ToAngleAxis(out angleOut, out axisOut);

angleOut *= Mathf.Deg2Rad;

Vector3 angularVelocity = (angleOut * axisOut) / Time.fixedDeltaTime;

//rotationText.text += "Debug: angularVelocity = " + angularVelocity.ToString("0.00") + "\n" + "\n";
//rotationText.text += "Debug: axisOut = " + axisOut.ToString("0.00") + "\n" + "\n";

//angularVelocity = (batRotationYDiff) / Time.fixedDeltaTime;

//rotationText.text += "Angular Velocity = " + angularVelocity.ToString("0.00");
*/