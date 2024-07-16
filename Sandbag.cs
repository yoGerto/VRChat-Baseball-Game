﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using UnityEngine.UI;
using Cysharp.Threading.Tasks.Triggers;
using System;
using Unity.Mathematics;
using UnityEngine.InputSystem.Controls;

public class Sandbag : UdonSharpBehaviour
{
    public GameObject[] batParts;
    public GameObject baseballBat;
    public TextMeshProUGUI rotationText;
    public Rigidbody sandbagRB;
    public Rigidbody batGhostRB;
    public Slider batWeight;
    public Button respawnSandbagButton;
    public Button launchSandbagButton;
    public TextMeshProUGUI batActualWeightText;
    public TextMeshProUGUI distanceTraveledText;
    public TextMeshProUGUI explosiveChargesText;
    public TextMeshProUGUI explosiveChargesText_RemainingCharges;

    public TextMeshProUGUI syncStatusDebug2;
    public TextMeshProUGUI syncStatusDebug3;

    public BatWeightSlider batWeightSlider;
    public BatFollower batFollower;

    private Vector3[] batVelocity, batPosCurr, batPosPrev;

    private Vector3 resultantImpulse = Vector3.zero;
    private Vector3 sandbagStartPos = Vector3.zero;
    //private Vector3 distanceOffset = Vector3.zero;

    private float m_ass; //mass in kg
    // Average baseball bat weighs between 0.96kg to 1.4kg

    private float timer, globalTimer, timeOffset = 0.0f;

    private const float cooldownTime = 1.0f;
    private const float respawnTimeout = 10.0f;

    private byte timerLatch = 0;

    private int currentSecond, previousSecond = 0;
    private int critChance = 50;

    private bool freezeBatGhost;
    private float freezeTimer;

    [UdonSynced, FieldChangeCallback(nameof(ExplosiveChargeExternalHandler))]private int explosiveCharges = 0;
    [UdonSynced] private int explosiveChargesLocal_totalPurchased = 0;
    [UdonSynced] private int explosiveChargesLocal_remainingAvailable = 0;

    [UdonSynced] private Vector3 storedMomentum = Vector3.zero;
    [UdonSynced] private bool[] bools = new bool[3];
    [UdonSynced, FieldChangeCallback(nameof(int_FieldChangeCallbackTest))] private int testingInt = 0;
    private bool[] boolsLocal = new bool[3];

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
            //collision.collider.enabled = false;

            // Determine the angle between 2 vectors: the contact normal and the velocity of the bat part
            float triangleSideA = Mathf.Sqrt( (contact.normal.x * contact.normal.x) + (contact.normal.z * contact.normal.z) );
            float triangleSideB = Mathf.Sqrt( (batVelocity[0].x * batVelocity[0].x) + (batVelocity[0].z * batVelocity[0].z));
            float triangleSideC = Mathf.Sqrt( (Mathf.Pow((batVelocity[0].x - contact.normal.x), 2)) + (Mathf.Pow((batVelocity[0].z - contact.normal.z), 2)) );

            float topOfAngleEquation = (triangleSideA * triangleSideA) + (triangleSideB * triangleSideB) - (triangleSideC * triangleSideC);
            float bottomOfAngleEquation = (2 * triangleSideA * triangleSideB);

            float angleBetweenNormalAndVelocity = Mathf.Acos(topOfAngleEquation / bottomOfAngleEquation);

            resultantImpulse.x = contact.normal.x * (triangleSideB * Mathf.Cos(angleBetweenNormalAndVelocity));
            resultantImpulse.y = batVelocity[0].y * Mathf.Cos(angleBetweenNormalAndVelocity);
            resultantImpulse.z = contact.normal.z * (triangleSideB * Mathf.Cos(angleBetweenNormalAndVelocity));

            //Debug.DrawRay(contact.normal, resultantImpulse, Color.green, 1.0f);
            //Debug.DrawRay(this.transform.position - contact.normal, contact.normal, Color.blue, 1.0f);
            //Debug.Log(contact.normal);

            int critRoll = UnityEngine.Random.Range(1, 101);

            if (critRoll <= critChance)
            {
                storedMomentum += (resultantImpulse * (m_ass / (float)batParts.Length)) * 2;
            }
            else
            {
                storedMomentum += resultantImpulse * (m_ass/ (float)batParts.Length);
            }

            rotationText.text = storedMomentum.ToString() + "\n";
            rotationText.text += "Crit chance = " + critChance + "\n";
            rotationText.text += "Did Crit roll? ";
            if (critRoll <= critChance)
            {
                rotationText.text += "YES!";
            }
            else
            {
                rotationText.text += "no :(";
            }

            //batGhostRB.constraints = RigidbodyConstraints.FreezeAll;
            //batFollower.SetProgramVariable("lockBat", true);
            //freezeBatGhost = true;

            RequestSerialization();
        }
    }
    public void LaunchSandbag()
    {
        // If the player who clicks the Launch Sandbag script does not own the Sandbag, they will not be able to set UdonSynced variables
        // I am experiencing this issue likely inpart because this script is too loaded
        // I will need to split this script up accordingly as managing ownership of this script is becoming too tedious
        if (Networking.GetOwner(this.gameObject) != Networking.LocalPlayer)
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }

        bools[0] = true;
        // This should trigger the setter on the int_FieldChangeCallbackTest property and increment the value of the variable it is attached to, even though on this line the value is being set to 0 (Needs testing?)
        int_FieldChangeCallbackTest = int_FieldChangeCallbackTest + 1;
        RequestSerialization();
    }
    public void LaunchSandbag_Networked()
    {
        timerLatch = 1;
        sandbagRB.constraints = RigidbodyConstraints.None;
        sandbagRB.velocity = storedMomentum;
    }

    public void RespawnSandbag()
    {
        if (Networking.GetOwner(this.gameObject) != Networking.LocalPlayer)
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }

        bools[1] = true;
        // This should trigger the setter on the int_FieldChangeCallbackTest property and increment the value of the variable it is attached to, even though on this line the value is being set to 0 (Needs testing?)
        int_FieldChangeCallbackTest = int_FieldChangeCallbackTest + 1;
        RequestSerialization();
    }

    public void RespawnSandbag_Networked()
    {
        timerLatch = 2;
        timer = 0.0f;

        if (Networking.GetOwner(this.gameObject) == Networking.LocalPlayer)
        {
            explosiveChargesLocal_remainingAvailable = explosiveChargesLocal_totalPurchased;
            RequestSerialization();
            OnDeserialization();
        }

        sandbagRB.velocity = Vector3.zero;
        sandbagRB.rotation = Quaternion.identity;
        sandbagRB.position = sandbagStartPos;
    }

    public void UseExplosive()
    {
        if (Networking.GetOwner(this.gameObject) != Networking.LocalPlayer)
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }

        if (explosiveChargesLocal_remainingAvailable > 0)
        {
            bools[2] = true;
            explosiveChargesLocal_remainingAvailable -= 1;
            int_FieldChangeCallbackTest = int_FieldChangeCallbackTest + 1;
            RequestSerialization();
        }

    }

    public void UseExplosive_Networked()
    {
        Vector3 currentVelocity = sandbagRB.velocity;
        currentVelocity.y += 30.0f;
        sandbagRB.velocity = currentVelocity;

        UpdateExplosiveUpgradeText();
    }

    public int int_FieldChangeCallbackTest
    {
        set 
        {
            if (Networking.GetOwner(this.gameObject) == Networking.LocalPlayer)
            {
                globalTimer = -0.25f;
            }
            else
            {
                globalTimer = 0.0f;
            }

            testingInt = value;

            for (int i = 0; i < bools.Length; i++)
            {
                // Cannot assign bools to boolsLocal directly because it seems to copy the UdonSynced property into it, which is not desired
                if (bools[i])
                {
                    boolsLocal[i] = true;
                }
                else
                {
                    boolsLocal[i] = false;
                }
            }
        }
        get { return testingInt; }
    }

    public int ExplosiveChargeExternalHandler
    {
        set 
        {
            if (Networking.GetOwner(this.gameObject) != Networking.LocalPlayer)
            {
                return;
            }

            int temp = explosiveChargesLocal_totalPurchased + value;
            explosiveChargesLocal_totalPurchased = temp;
            explosiveChargesLocal_remainingAvailable = temp;
            RequestSerialization();
            OnDeserialization();
        }
        get { return explosiveCharges; }
    }

    void Start()
    {
        batPosCurr = new Vector3[batParts.Length];
        batPosPrev = new Vector3[batParts.Length];
        batVelocity = new Vector3[batParts.Length];

        sandbagRB = GetComponent<Rigidbody>();
        sandbagStartPos = sandbagRB.position;

        currentSecond = (int)Math.Truncate(Time.realtimeSinceStartup);
        previousSecond = currentSecond;
    }

    private void FixedUpdate()
    {
        // Create a bit mask that disables RayCast collisions with layer 22 (BatPartCollider) and layer 13 (Pickup)
        int layerMask = (1 << 22) + (1 << 13);

        // The mask we created previously needs to be flipped so everything other than the layers we defined ealier recieves collisions from the RayCast
        layerMask = ~layerMask;

        m_ass = batWeight.value;
        batActualWeightText.text = m_ass.ToString("0.0") + "kg";

        // I want to put this here to hopefully catch any case where these two array lengths are different
        if (batParts.Length != batPosCurr.Length)
        {
            Debug.LogError("batParts and batPosCurr are of different length! Program will crash");
        }

        RaycastHit hit0;
        
        //batPosCurr[0] = batParts[0].transform.position;
        batPosCurr[0] = baseballBat.transform.GetChild(0).transform.GetChild(0).transform.position;

        //Debug.Log("Child 0 pos = " + baseballBat.transform.GetChild(0).transform.position);
        Debug.Log("Child 0 pos = " + baseballBat.transform.GetChild(0).transform.GetChild(0).transform.position);
        //Debug.Log("Child 1 pos = " + baseballBat.transform.GetChild(1).transform.position);

        if (Physics.Raycast(batPosPrev[0], (batPosCurr[0] - batPosPrev[0]), out hit0, (batPosCurr[0] - batPosPrev[0]).magnitude, layerMask))
        {
            Debug.DrawRay(hit0.point, hit0.normal, Color.blue, 1.0f);
            Debug.Log("in hit0");
            Debug.Log("Child 0 pos = " + baseballBat.transform.GetChild(0).transform.GetChild(0).transform.position);
        }

        batVelocity[0] = (batPosCurr[0] - batPosPrev[0]) / Time.fixedDeltaTime;
        batPosPrev[0] = batPosCurr[0];

        
        RaycastHit hit1;

        batPosCurr[1] = baseballBat.transform.GetChild(0).transform.GetChild(1).transform.position;

        if (Physics.Raycast(batPosPrev[1], (batPosCurr[1] - batPosPrev[1]), out hit1, (batPosCurr[1] - batPosPrev[1]).magnitude, layerMask))
        {
            Debug.DrawRay(hit1.point, hit1.normal, Color.blue, 1.0f);
            Debug.Log("in hit1");
            Debug.Log("Child 1 pos = " + baseballBat.transform.GetChild(0).transform.GetChild(1).transform.position);
        }

        batVelocity[1] = (batPosCurr[1] - batPosPrev[1]) / Time.fixedDeltaTime;
        batPosPrev[1] = batPosCurr[1];

        if (timerLatch == 1)
        {
            timer += Time.deltaTime;
            if (timer > cooldownTime + 0.02f)
            {
                storedMomentum = Vector3.zero;
                for (int i = 0; i < batParts.Length; i++)
                {
                    batParts[i].GetComponent<Collider>().enabled = true;
                }
            }
            if (timer >= respawnTimeout)
            {
                Vector2 distanceOffset = Vector2.zero;
                float distanceTraveled = 0.0f;
                distanceOffset[0] = sandbagRB.position.x - sandbagStartPos.x;
                distanceOffset[1] = sandbagRB.position.z - sandbagStartPos.z;
                distanceTraveled = distanceOffset.magnitude;

                distanceTraveledText.text = distanceTraveled.ToString();

                batWeightSlider.SetProgramVariable("moneyFromOutside", distanceTraveled);

                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RespawnSandbag_Networked");
            }
        }
        else if (timerLatch == 2)
        {
            timer += Time.deltaTime;
            if (timer > 0.1f)
            {
                sandbagRB.constraints = RigidbodyConstraints.FreezeAll;
                timer = 0.0f;
            }
        }
    }

    private void Update()
    {
        if (globalTimer < 1.0f)
        {
            globalTimer += Time.deltaTime;

        }

        if (freezeBatGhost)
        {
            freezeTimer += Time.deltaTime;
            if (freezeTimer > 0.5)
            {
                //batGhostRB.constraints = RigidbodyConstraints.None;
                batFollower.SetProgramVariable("lockBat", false);
                freezeTimer = 0.0f;
                freezeBatGhost = false;
            }
        }

        currentSecond = (int)Math.Truncate(Time.realtimeSinceStartup + timeOffset);

        syncStatusDebug3.text = "bools[0] = " + bools[0].ToString() + "\n";
        syncStatusDebug3.text += "boolsLocal[0] = " + boolsLocal[0].ToString() + "\n";

        syncStatusDebug3.text += "globalTimer = " + globalTimer.ToString() + "\n";
        syncStatusDebug3.text += "testingInt = " + testingInt.ToString() + "\n";

        syncStatusDebug2.text = "timeOffset = " + timeOffset.ToString() + "\n";
        syncStatusDebug2.text += (Time.realtimeSinceStartup + timeOffset).ToString() + "\n";
        syncStatusDebug2.text += currentSecond.ToString() + "\n";
        //syncStatusDebug2.text += explosiveChargesLocal.ToString() + "\n";
        //syncStatusDebug2.text += previousSecond.ToString() + "\n";

        if (currentSecond != previousSecond)
        {
            if (globalTimer > 0.25)
            {
                if (boolsLocal[0])
                {
                    LaunchSandbag_Networked();
                    boolsLocal[0] = false;
                    if (Networking.GetOwner(this.gameObject) == Networking.LocalPlayer)
                    {
                        bools[0] = false;
                    }
                }
                if (boolsLocal[1])
                {
                    RespawnSandbag_Networked();
                    boolsLocal[1] = false;
                    if (Networking.GetOwner(this.gameObject) == Networking.LocalPlayer)
                    {
                        bools[1] = false;
                    }
                    //UpdateExplosiveUpgradeText();
                }
                if (boolsLocal[2])
                {
                    UseExplosive_Networked();
                    boolsLocal[2] = false;
                    if (Networking.GetOwner(this.gameObject) == Networking.LocalPlayer)
                    {
                        bools[2] = false;
                    }
                }
            }
            RequestSerialization();
        }

        previousSecond = currentSecond;
    }

    
    public override void OnDeserialization()
    {
        UpdateExplosiveUpgradeText();
    }

    public void UpdateExplosiveUpgradeText()
    {
        explosiveChargesText.text = explosiveChargesLocal_totalPurchased.ToString();
        explosiveChargesText_RemainingCharges.text = explosiveChargesLocal_remainingAvailable.ToString();
    }

}