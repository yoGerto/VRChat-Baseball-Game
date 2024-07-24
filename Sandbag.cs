
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
    public GameObject baseballBatGhost;
    public GameObject[] debugHitSpheres;

    public GameObject floatingTextPrefab;

    public TextMeshProUGUI rotationText;
    public Rigidbody sandbagRB;
    public Rigidbody batRB;
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
    public BatShake batShakeScript;

    private Vector3[] batVelocity, batPosCurr, batPosPrev;
    private bool[] batPartHasSwung;

    private Vector3 resultantImpulse = Vector3.zero;
    private Vector3 sandbagStartPos = Vector3.zero;
    //private Vector3 distanceOffset = Vector3.zero;

    private float m_ass; //mass in kg
    // Average baseball bat weighs between 0.96kg to 1.4kg

    private float timer, globalTimer, timeOffset = 0.0f;

    private const float cooldownTime = 1.0f;
    private const float respawnTimeout = 10.0f;

    private byte timerLatch = 0;

    private Vector3 batTransformPosCurrent = Vector3.zero;
    private Vector3 batTransformPosPrevious = Vector3.zero;

    private int currentSecond, previousSecond = 0;
    private int critChance = 50;

    // Create a bit mask that disables RayCast collisions with layer 22 (BatPartCollider) and layer 13 (Pickup)
    int layerMask;

    private bool isBatHeld = false;
    private bool freezeBatGhost;
    private float freezeTimer;

    private float yetAnotherTimer = 0.0f;
    private bool yetAnotherBool = false;

    private float recentDamage = 0.0f;

    VRCPlayerApi player;
    GameObject damagetext = null;

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

        for (int i = 0; i < batParts.Length; i++)
        {
            batPartHasSwung[i] = false;
        }

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

        batPartHasSwung = new bool[batParts.Length];
        for (int i = 0;i < batParts.Length;i++)
        {
            batPartHasSwung[i] = false;
        }

        sandbagRB = GetComponent<Rigidbody>();
        sandbagStartPos = sandbagRB.position;

        currentSecond = (int)Math.Truncate(Time.realtimeSinceStartup);
        previousSecond = currentSecond;

        layerMask = (1 << 22) + (1 << 17) + (1 << 13);
        // The mask we created previously needs to be flipped so everything other than the layers we defined ealier recieves collisions from the RayCast
        layerMask = ~layerMask;

        player = Networking.LocalPlayer;
    }

    private void FixedUpdate()
    {
        m_ass = batWeight.value;
        batActualWeightText.text = m_ass.ToString("0.0") + "kg";

        RaycastHit hit;

        batTransformPosCurrent = baseballBat.transform.position;

        for (int i = 0; i < batParts.Length; i++)
        {
            batPosCurr[i] = baseballBat.transform.GetChild(0).transform.GetChild(i).transform.position;
            batVelocity[i] = (batPosCurr[i] - batPosPrev[i]) / Time.fixedDeltaTime;

            // Only do RayCasts whilst bat is being held
            // Need to look up how to make this section neater as I have heard multiple nested ifs is bad practise
            if (isBatHeld)
            {
                if (!batPartHasSwung[i])
                {
                    if (Physics.Raycast(batPosPrev[i], (batPosCurr[i] - batPosPrev[i]), out hit, (batPosCurr[i] - batPosPrev[i]).magnitude, layerMask))
                    {
                        if (yetAnotherBool == false)
                        {
                            baseballBat.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                            batShakeScript.SetProgramVariable("start", true);
                            yetAnotherBool = true;
                            Debug.DrawLine(batPosPrev[i], batPosCurr[i], Color.red, 1.0f);
                            //Debug.DrawRay(hit.point, hit.normal, Color.blue, 1.0f);
                            //Debug.DrawRay(hit.point, hit.normal * -1, Color.red, 1.0f);
                            //.DrawRay(hit.point, batVelocity[i], Color.green, 1.0f);
                            //Debug.Log("hit.point = " + hit.point);
                            //Debug.Log("baseballBat pos = " + baseballBatGhost.transform.position);
                            var vectDif = (hit.point - batPosCurr[i]).normalized;

                            baseballBatGhost.transform.position = baseballBatGhost.transform.position;// - (batPosCurr[i] - hit.point);
                            debugHitSpheres[0].transform.position = hit.point;
                            debugHitSpheres[1].transform.position = batPosCurr[i];
                            debugHitSpheres[2].transform.position = batPosPrev[i];
                            debugHitSpheres[3].transform.position = baseballBatGhost.transform.position;
                            debugHitSpheres[4].transform.position = hit.point + (vectDif * 0.05f);

                            Vector3 glorp = hit.point + (vectDif * 0.05f);
                            Vector3 glorp2 = glorp;

                            Vector2 absolution = Vector2.zero;
                            absolution.x = batPosPrev[i].x - batPosCurr[i].x;
                            absolution.y = batPosPrev[i].z - batPosCurr[i].z;
                            Vector2 retribution = new Vector2(absolution.y, -absolution.x);
                            Debug.Log("absolution = " + absolution);
                            Debug.Log("retribution = " + retribution);

                            glorp2.x += retribution.x;
                            glorp2.z += retribution.y;

                            Debug.Log("batPosPrev = " + batPosPrev[i]);
                            Debug.Log("batPosCurr = " + batPosCurr[i]);

                            Vector3 playerCoords = player.GetPosition();
                            playerCoords.y = glorp.y;

                            Debug.DrawLine(glorp, playerCoords, Color.red, 1.0f);

                            //Debug.Log("baseballBat pos = " + baseballBatGhost.transform.position);
                            //Debug.DrawLine(temp1, temp2, Color.red, 1.0f);
                            //Debug.DrawLine(temp1, baseballBatGhost.transform.position, Color.red, 1.0f);
                            //baseballBatGhost.transform.position = batTransformPosPrevious;
                            //baseballBatGhost.transform.position = hit.point - ;
                        }

                        // damagetext becomes null when the object instantiated to it is destroyed
                        // This can be used to not only contain a reference to the instantiated object, but also as a flag to check if floating damage text already exists
                        // If it does already exist, use the Play method on the object's Animator to reset the floating animation to the start
                        if (damagetext == null)
                        {
                            recentDamage = 0.0f;
                            damagetext = Instantiate(floatingTextPrefab, this.transform);
                            damagetext.transform.position = transform.position + new Vector3(0.0f, 1.5f, 0.0f);
                            damagetext.transform.GetChild(0).GetComponent<Animator>().Play("TextFloatAnimation", 0, 0.0f);
                        }
                        else
                        {
                            damagetext.transform.GetChild(0).GetComponent<Animator>().Play("TextFloatAnimation", 0, 0.0f);
                        }
  
                        //batPartHasSwung[i] = true;

                        // If we are here, that means the bat has made contact with the sandbag (presumably)
                        // The local player needs to be the owner of the Sandbag to update the networked variables, so make them the owner
                        if (Networking.GetOwner(this.gameObject) != Networking.LocalPlayer)
                        {
                            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                        }

                        // Invert the collision normal so it points to the centre of the Sandbag (roughly to the centre of mass)
                        Vector3 hitNormalInverted = hit.normal * -1;

                        // Use Phythagoras theorem to calculate the unknown third side of a triangle
                        // The triangle is a 2d triangle using the x and z components of the inverted hit normal and bat velocity
                        float triangleSideA = Mathf.Sqrt((hitNormalInverted.x * hitNormalInverted.x) + (hitNormalInverted.z * hitNormalInverted.z));
                        float triangleSideB = Mathf.Sqrt((batVelocity[i].x * batVelocity[i].x) + (batVelocity[i].z * batVelocity[i].z));
                        float triangleSideC = Mathf.Sqrt((Mathf.Pow((batVelocity[i].x - hitNormalInverted.x), 2)) + (Mathf.Pow((batVelocity[i].z - hitNormalInverted.z), 2)));

                        // Create the two sides of the equation to make it easier to see the equation
                        float topOfAngleEquation = (triangleSideA * triangleSideA) + (triangleSideB * triangleSideB) - (triangleSideC * triangleSideC);
                        float bottomOfAngleEquation = (2 * triangleSideA * triangleSideB);

                        // Find the angle between the normal and velocity '2D' vectors
                        float angleBetweenNormalAndVelocity = Mathf.Acos(topOfAngleEquation / bottomOfAngleEquation);

                        // Use the angle to determine how much of the velocity should be used
                        resultantImpulse.x = hitNormalInverted.x * (triangleSideB * Mathf.Cos(angleBetweenNormalAndVelocity));
                        resultantImpulse.y = batVelocity[i].y * Mathf.Cos(angleBetweenNormalAndVelocity);
                        resultantImpulse.z = hitNormalInverted.z * (triangleSideB * Mathf.Cos(angleBetweenNormalAndVelocity));

                        // Roll for crit
                        int critRoll = UnityEngine.Random.Range(1, 101);

                        if (critRoll <= critChance)
                        {
                            //Debug.DrawRay(hit.point, resultantImpulse * 2, Color.yellow, 1.0f);
                            storedMomentum += (resultantImpulse * (m_ass / (float)batParts.Length)) * 2;
                            recentDamage += ((resultantImpulse * (m_ass / (float)batParts.Length)) * 2).magnitude;
                            damagetext.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = recentDamage.ToString("0.00");
                        }
                        else
                        {
                            //Debug.DrawRay(hit.point, resultantImpulse, Color.magenta, 1.0f);
                            storedMomentum += resultantImpulse * (m_ass / (float)batParts.Length);
                            recentDamage += ((resultantImpulse * (m_ass / (float)batParts.Length))).magnitude;
                            damagetext.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = recentDamage.ToString("0.00");
                        }

                        RequestSerialization();
                        OnDeserialization();

                        //Debug.DrawRay(hit.point, resultantImpulse, Color.magenta, 1.0f);
                        /*
                        Debug.DrawRay(hit.point, hit.normal, Color.blue, 1.0f);
                        Debug.DrawRay(hit.point, hit.normal * -1, Color.red, 1.0f);
                        Debug.DrawRay(hit.point, batVelocity[i], Color.green, 1.0f);
                        Debug.Log("hit.point = "+ hit.point);
                        Debug.Log("baseballBat pos = " + baseballBatGhost.transform.position);
                        */
                    }
                }
            }
            batPosPrev[i] = batPosCurr[i];
        }

        batTransformPosPrevious = batTransformPosCurrent;

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
        if (!yetAnotherBool)
        {
            baseballBatGhost.transform.position = baseballBat.transform.position;
            baseballBatGhost.transform.rotation = baseballBat.transform.rotation;
        }
        else
        {
            yetAnotherTimer += Time.deltaTime;
            if (yetAnotherTimer > 4.0f)
            {
                baseballBat.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                yetAnotherTimer = 0.0f;
                yetAnotherBool = false;
            }
        }

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

        syncStatusDebug3.text = "yetAnotherBool = " + yetAnotherBool.ToString() + "\n";

        //syncStatusDebug3.text = "bools[0] = " + bools[0].ToString() + "\n";
        //syncStatusDebug3.text += "boolsLocal[0] = " + boolsLocal[0].ToString() + "\n";

        //syncStatusDebug3.text += "globalTimer = " + globalTimer.ToString() + "\n";
        //syncStatusDebug3.text += "testingInt = " + testingInt.ToString() + "\n";

        //syncStatusDebug2.text = "timeOffset = " + timeOffset.ToString() + "\n";
        //syncStatusDebug2.text += (Time.realtimeSinceStartup + timeOffset).ToString() + "\n";
        //syncStatusDebug2.text += currentSecond.ToString() + "\n";
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
        rotationText.text = storedMomentum.ToString();
        UpdateExplosiveUpgradeText();
    }

    public void UpdateExplosiveUpgradeText()
    {
        explosiveChargesText.text = explosiveChargesLocal_totalPurchased.ToString();
        explosiveChargesText_RemainingCharges.text = explosiveChargesLocal_remainingAvailable.ToString();
    }

}