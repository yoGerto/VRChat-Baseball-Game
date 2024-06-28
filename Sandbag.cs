﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using UnityEngine.UI;
using Cysharp.Threading.Tasks.Triggers;

public class Sandbag : UdonSharpBehaviour
{
    public GameObject[] batParts;
    public TextMeshProUGUI rotationText;
    public Rigidbody sandbagRB;
    public Slider batWeight;
    public Button respawnSandbagButton;
    public TextMeshProUGUI batActualWeightText;

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

    private float m_ass = 1.4f; //mass in kg
    // I know this isnt proper naming convention but it says ass lol
    // Average baseball bat weighs between 0.96kg to 1.4kg
    private float sandbagWeight = 20.0f;
    private Vector3 sandbagStartPos = Vector3.zero;

    private float timer = 0.0f;
    private float cooldownTime = 1.0f;
    private int timerLatch = 0;

    private Vector3 storedMomentum = Vector3.zero;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.name == "Floor")
        {
            return;
        }

        foreach (ContactPoint contact in collision.contacts)
        {
            // Disable the collider on contact so it does not continue to collide with the sandbag
            collision.collider.enabled = false;

            // Set the timerLatch to 1, which will release the sandbag with the stored velocity after 1 second
            timerLatch = 1;

            // Get the contact normal and multiply it by the linear momentum vector to determine the resultant impulse
            rotationText.text = "batVelocity = " + batVelocity.ToString() + "\n";
            rotationText.text += "contact.normal = " + contact.normal.ToString() + "\n";

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

            rotationText.text += "resultantImpulse = " + resultantImpulse.ToString();

            //Debug.Log("batVelocity = " + batVelocity.ToString());
            //Debug.Log("contact.normal = " + contact.normal.ToString());

            storedMomentum += resultantImpulse * (m_ass/ (float)batParts.Length);

            Debug.DrawRay(contact.point, batVelocity, Color.green, 1.0f);
            Debug.DrawRay(contact.point, resultantImpulse, Color.blue, 1.0f);
            Debug.DrawRay(contact.point, contact.normal, Color.red, 1.0f);
        }
    }


    public void RespawnSandbag()
    {
        timerLatch = 2;
        timer = 0.0f;

        sandbagRB.velocity = Vector3.zero;
        sandbagRB.rotation = Quaternion.identity;
        sandbagRB.position = sandbagStartPos;
    }


    void Start()
    {
        sandbagRB = GetComponent<Rigidbody>();
        sandbagStartPos = sandbagRB.position;
    }

    private void FixedUpdate()
    {
        m_ass = batWeight.value;
        batActualWeightText.text = m_ass.ToString("0.0") + "kg";

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
            if (timer > cooldownTime)
            {
                sandbagRB.constraints = RigidbodyConstraints.None;
                sandbagRB.velocity = storedMomentum;
                //sandbagRB.AddForce(storedMomentum, ForceMode.Force);

            }
            if (timer > cooldownTime + 0.02f)
            {
                storedMomentum = Vector3.zero;
                Debug.Log(batParts.Length);
                for (int i = 0; i < batParts.Length - 1; i++)
                {
                    batParts[i].GetComponent<Collider>().enabled = true;
                }
                timerLatch = 0;
                timer = 0.0f;
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
}