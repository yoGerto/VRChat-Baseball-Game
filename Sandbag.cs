
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
    private Vector3 batTransformVelocity = Vector3.zero;

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
        batTransformVelocity = (batTransformPosCurrent - batTransformPosPrevious) / Time.fixedDeltaTime;
        batTransformPosPrevious = batTransformPosCurrent;

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
                        // Find the difference between hit.point and batPosCurr
                        var hitDiff = batPosCurr[i] - hit.point;
                        // Normalize this distance to get a unit direciton
                        var hitDiffNormalized = hitDiff.normalized;
                        // Find the new difference of a second point, which is the ideal transform point
                        // This point is the hit distance minus a fraction of hitDiffNormalized, which is a unit line drawn from hitPosCurr to hit.point
                        // Subtracting a fraction of this hitDiffNormalized variable creates a Vector3 position similar to the hit.position but it is slightly outside the sandbag
                        var idealTransformPoint = hit.point - (hitDiffNormalized * 0.05f);
                        // Then calculate this new difference in distance between the bat part that hit and the idealTransformPoint
                        var newDiff = batPosCurr[i] - idealTransformPoint;

                        // Calculate a factor of the ratio between how fast the bat is moving versus how fast the bat part that makes the collision is moving
                        // If the bat part is moving faster than the bat (which it will under normal circumstances) then the factor will be less than 1
                        // This factor is used to move the batTransform by a suitable amount based on how fast the bat part is moving
                        // This calculation should let the code work with any bat part along the bat, so long as I have the velocity of that part.
                        var inverseFactor = (batTransformVelocity.magnitude / batVelocity[0].magnitude);

                        if (yetAnotherBool == false)
                        {
                            batShakeScript.SetProgramVariable("start", true);
                            yetAnotherBool = true;

                            baseballBatGhost.transform.position = baseballBat.transform.position - (newDiff * inverseFactor);
                            baseballBatGhost.transform.LookAt(idealTransformPoint, Vector3.up);
                            // ...There has to be a cleaner way to do this but this gets me what I want for now
                            baseballBatGhost.transform.rotation = Quaternion.Euler(baseballBatGhost.transform.eulerAngles.x + 90, baseballBatGhost.transform.eulerAngles.y, baseballBatGhost.transform.eulerAngles.z);
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
  
                        batPartHasSwung[i] = true;

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
                            storedMomentum += (resultantImpulse * (m_ass / (float)batParts.Length)) * 2;
                            recentDamage += ((resultantImpulse * (m_ass / (float)batParts.Length)) * 2).magnitude;
                            damagetext.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = recentDamage.ToString("0.00");
                        }
                        else
                        {
                            storedMomentum += resultantImpulse * (m_ass / (float)batParts.Length);
                            recentDamage += ((resultantImpulse * (m_ass / (float)batParts.Length))).magnitude;
                            damagetext.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = recentDamage.ToString("0.00");
                        }

                        RequestSerialization();
                        OnDeserialization();
                    }
                }
            }
            batPosPrev[i] = batPosCurr[i];
        }


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
            if (yetAnotherTimer > 1.0f)
            {
                yetAnotherTimer = 0.0f;
                yetAnotherBool = false;
            }
        }

        if (globalTimer < 1.0f)
        {
            globalTimer += Time.deltaTime;

        }

        currentSecond = (int)Math.Truncate(Time.realtimeSinceStartup + timeOffset);

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