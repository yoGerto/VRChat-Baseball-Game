
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
    [SerializeField] private Rigidbody sandbagRB;

    public TextMeshProUGUI syncStatusDebug2;
    public TextMeshProUGUI syncStatusDebug3;

    public BatWeightSlider batWeightSlider;

    private Vector3 sandbagStartPos = Vector3.zero;

    private float m_ass; //mass in kg
    // Average baseball bat weighs between 0.96kg to 1.4kg

    private float timer, globalTimer, timeOffset = 0.0f;

    private const float cooldownTime = 1.0f;
    private const float respawnTimeout = 10.0f;

    private byte timerLatch = 0;

    private int currentSecond, previousSecond = 0;

    public UpdateHasSwungState updateSwingStateScript;

    VRCPlayerApi player;

    [UdonSynced, FieldChangeCallback(nameof(ExplosiveChargeExternalHandler))]private int explosiveCharges = 0;
    [UdonSynced] private int explosiveChargesLocal_totalPurchased = 0;
    [UdonSynced] private int explosiveChargesLocal_remainingAvailable = 0;

    [UdonSynced, FieldChangeCallback(nameof(External_StoredMomentum))] private Vector3 storedMomentum = Vector3.zero;
    [UdonSynced] private Vector3 storedMomentumLocal = Vector3.zero;
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
        sandbagRB.velocity = storedMomentumLocal;
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

        updateSwingStateScript.SendCustomEvent("SwingStateReset");

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

    public Vector3 External_StoredMomentum
    {
        set
        {
            if (Networking.GetOwner(this.gameObject) != Networking.LocalPlayer)
            {
                return;
            }
            var temp = storedMomentumLocal + value;
            storedMomentumLocal = temp;
            RequestSerialization();
            OnDeserialization();
        }
        get { return storedMomentum; }
    }

    void Start()
    {
        sandbagRB = GetComponent<Rigidbody>();
        sandbagStartPos = sandbagRB.position;

        currentSecond = (int)Math.Truncate(Time.realtimeSinceStartup);
        previousSecond = currentSecond;

        player = Networking.LocalPlayer;
    }

    private void FixedUpdate()
    {
        if (timerLatch == 1)
        {
            timer += Time.deltaTime;
            if (timer > cooldownTime + 0.02f)
            {

                storedMomentumLocal = Vector3.zero;
            }
            if (timer >= respawnTimeout)
            {
                Vector2 distanceOffset = Vector2.zero;
                float distanceTraveled = 0.0f;
                distanceOffset[0] = sandbagRB.position.x - sandbagStartPos.x;
                distanceOffset[1] = sandbagRB.position.z - sandbagStartPos.z;
                distanceTraveled = distanceOffset.magnitude;

                // Change this from a 'moneyFromOutside' to a 'distanceTraveled' then calculate the money and set the UI values inside that script instead.
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

        currentSecond = (int)Math.Truncate(Time.realtimeSinceStartup + timeOffset);

        if (currentSecond != previousSecond)
        {
            Debug.Log(storedMomentumLocal);
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
        //explosiveChargesText.text = explosiveChargesLocal_totalPurchased.ToString();
        //explosiveChargesText_RemainingCharges.text = explosiveChargesLocal_remainingAvailable.ToString();
    }

}