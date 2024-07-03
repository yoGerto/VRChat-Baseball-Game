
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using UnityEngine.UI;
using Cysharp.Threading.Tasks.Triggers;
using System;

public class Sandbag : UdonSharpBehaviour
{
    public GameObject[] batParts;
    public TextMeshProUGUI rotationText;
    public Rigidbody sandbagRB;
    public Slider batWeight;
    public Button respawnSandbagButton;
    public Button launchSandbagButton;
    public TextMeshProUGUI batActualWeightText;
    public TextMeshProUGUI distanceTraveledText;

    public TextMeshProUGUI syncStatusDebug2;
    public TextMeshProUGUI syncStatusDebug3;

    private Vector3 batVelocity = Vector3.zero;
    private Vector3 batPosCurr = Vector3.zero;
    private Vector3 batPosPrev = Vector3.zero;

    private Vector3 resultantImpulse = Vector3.zero;

    private Quaternion batRotationCurr = Quaternion.identity;
    private Quaternion batRotationPrev = Quaternion.identity;
    private Quaternion batRotationDelta = Quaternion.identity;

    private float linearMomentum;

    //private float batRotationYCurr = 0.0f;
    //private float batRotationYPrev = 0.0f;
    //private float batRotationYDiff = 0.0f;

    private float m_ass; //mass in kg
    // Average baseball bat weighs between 0.96kg to 1.4kg

    private float sandbagWeight = 20.0f;
    private Vector3 sandbagStartPos = Vector3.zero;

    private Vector3 distanceOffset = Vector3.zero;
    private float distanceTraveled = 0.0f;

    private float timer = 0.0f;
    private float globalTimer = 0.0f;
    private float cooldownTime = 1.0f;
    private int timerLatch = 0;
    private float timeOffset = 0.0f;

    private int currentSecond = 0;
    private int previousSecond = 0;
    private int counter = 0;

    [UdonSynced] private Vector3 storedMomentum = Vector3.zero;
    [UdonSynced] private bool[] bools = new bool[2];

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.name == "Floor")
        {
            return;
        }
        if (collision.collider.name == "Big Floor Test")
        {
            sandbagRB.velocity *= (float)0.9;
            return;
        }

        foreach (ContactPoint contact in collision.contacts)
        {
            // Disable the collider on contact so it does not continue to collide with the sandbag
            collision.collider.enabled = false;

            // Set the timerLatch to 1, which will release the sandbag with the stored velocity after 1 second
            //timerLatch = 1;

            // Get the contact normal and multiply it by the linear momentum vector to determine the resultant impulse
            //rotationText.text = "batVelocity = " + batVelocity.ToString() + "\n";
            //rotationText.text += "contact.normal = " + contact.normal.ToString() + "\n";

            // Determine the angle between 2 vectors: the contact normal and the velocity of the bat part
            float triangleSideA = Mathf.Sqrt( (contact.normal.x * contact.normal.x) + (contact.normal.z * contact.normal.z) );
            float triangleSideB = Mathf.Sqrt( (batVelocity.x * batVelocity.x) + (batVelocity.z * batVelocity.z));
            float triangleSideC = Mathf.Sqrt( (Mathf.Pow((batVelocity.x - contact.normal.x), 2)) + (Mathf.Pow((batVelocity.z - contact.normal.z), 2)) );

            float topOfAngleEquation = (triangleSideA * triangleSideA) + (triangleSideB * triangleSideB) - (triangleSideC * triangleSideC);
            float bottomOfAngleEquation = (2 * triangleSideA * triangleSideB);

            float angleBetweenNormalAndVelocity = Mathf.Acos(topOfAngleEquation / bottomOfAngleEquation);
            //Debug.Log("angle = " + angleBetweenNormalAndVelocity * (180/Mathf.PI));

            //Vector3 resultantImpulse = Vector3.zero;

            resultantImpulse.x = contact.normal.x * (triangleSideB * Mathf.Cos(angleBetweenNormalAndVelocity));
            resultantImpulse.y = batVelocity.y * Mathf.Cos(angleBetweenNormalAndVelocity);
            resultantImpulse.z = contact.normal.z * (triangleSideB * Mathf.Cos(angleBetweenNormalAndVelocity));

            //Vector3 resultantImpulse = contact.normal * (triangleSideB * Mathf.Cos(angleBetweenNormalAndVelocity));

            //rotationText.text += "resultantImpulse = " + resultantImpulse.ToString();

            //Debug.Log("batVelocity = " + batVelocity.ToString());
            //Debug.Log("contact.normal = " + contact.normal.ToString());

            storedMomentum += resultantImpulse * (m_ass/ (float)batParts.Length);

            Debug.DrawRay(contact.point, batVelocity, Color.green, 1.0f);
            Debug.DrawRay(contact.point, resultantImpulse, Color.blue, 1.0f);
            Debug.DrawRay(contact.point, contact.normal, Color.red, 1.0f);
        }
    }


    public void GetTimeOffset(float passedValue)
    {
        Debug.Log("This function has been entered");
        Debug.Log(passedValue);
        timeOffset = passedValue;
        syncStatusDebug2.text = timeOffset.ToString();
    }


    public void RespawnSandbag()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RespawnSandbag_Networked");
    }

    public void RespawnSandbag_Networked()
    {
        timerLatch = 2;
        timer = 0.0f;

        sandbagRB.velocity = Vector3.zero;
        sandbagRB.rotation = Quaternion.identity;
        sandbagRB.position = sandbagStartPos;
    }

    public void LaunchSandbag()
    {
        bools[0] = true;
        //SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchSandbag_Networked");
    }


    public void LaunchSandbag_Networked()
    {
        timerLatch = 1;
        sandbagRB.constraints = RigidbodyConstraints.None;
        sandbagRB.velocity = storedMomentum;
    }

    void Start()
    {
        sandbagRB = GetComponent<Rigidbody>();
        sandbagStartPos = sandbagRB.position;

        currentSecond = (int)Math.Truncate(Time.realtimeSinceStartup);
        previousSecond = currentSecond;
    }

    private void FixedUpdate()
    {
        m_ass = batWeight.value;
        batActualWeightText.text = m_ass.ToString("0.0") + "kg";

        rotationText.text = storedMomentum.ToString();

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


        if (timerLatch == 1)
        {
            timer += Time.deltaTime;
            if (timer > cooldownTime + 0.02f)
            {
                storedMomentum = Vector3.zero;
                Debug.Log(batParts.Length);
                for (int i = 0; i < batParts.Length; i++)
                {
                    batParts[i].GetComponent<Collider>().enabled = true;
                }
            }
            if (timer > 10.0f)
            {
                distanceOffset = sandbagRB.position - sandbagStartPos;
                distanceTraveled = distanceOffset.magnitude;
                distanceTraveledText.text = distanceTraveled.ToString();
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RespawnSandbag_Networked");
            }
        }
        else if (timerLatch == 2)
        {
            timer += Time.deltaTime;
            if (timer > 0.5)
            {
                sandbagRB.constraints = RigidbodyConstraints.FreezeAll;
                timer = 0.0f;
            }
        }
        else
        {

        }
        
    }

    private void Update()
    {
        syncStatusDebug3.text = "bools[0] = " + bools[0].ToString();

        syncStatusDebug2.text = "timeOffset = " + timeOffset.ToString() + "\n";
        syncStatusDebug2.text += (Time.realtimeSinceStartup + timeOffset).ToString() + "\n";
        currentSecond = (int)Math.Truncate(Time.realtimeSinceStartup + timeOffset);
        syncStatusDebug2.text += currentSecond.ToString() + "\n";
        syncStatusDebug2.text += previousSecond.ToString() + "\n";
        if (currentSecond != previousSecond)
        {
            if (bools[0])
            {
                LaunchSandbag_Networked();
                bools[0] = false;
            }
            counter++;
        }
        syncStatusDebug2.text += counter.ToString() + "\n";

        previousSecond = currentSecond;
    }
}