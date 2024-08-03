
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using System;
using UnityEngine.UI;


public enum Weapon
{
    Bat,
    Katana
}

public class WeaponScript : UdonSharpBehaviour
{
    public GameObject sandbag;
    public GameObject baseballBatGhost;
    public GameObject weaponPartsParent;
    public GameObject floatingTextPrefab;
    public Slider weaponWeight;

    [SerializeField] BatShake batShakeScript;
    [SerializeField] GameObject godTest;
    int weaponPartsCount;
    Transform[] weaponParts;
    Transform weapon;
    GameObject damagetext = null;

    private Vector3[] weaponVelocity, weaponPosCurr, weaponPosPrev;
    private Vector3 weaponTransformPosCurrent, weaponTransformPosPrevious, weaponTransformVelocity, resultantImpulse = Vector3.zero;

    private bool[] weaponPartHasSwung;
    private bool isHeld = false;
    [SerializeField] public bool yetAnotherBool = false;

    [SerializeField] private float yetAnotherTimer, recentDamage = 0.0f;
    private float m_ass = 1.4f;

    private int critChance = 50;
    private const int layerMask = (1 << 23);

    VRCPlayerApi player;
    Sandbag sandbagScript;

    public Weapon weaponType;
    // Bat == 0
    // Katana == 1

    
    void Start()
    {
        if (weaponType == 0)
        {
            godTest = baseballBatGhost.transform.GetChild(0).gameObject;
            batShakeScript = baseballBatGhost.transform.GetChild(0).GetComponent<BatShake>();
        }

        weaponPartsCount = weaponPartsParent.transform.childCount;

        weapon = transform.root;

        weaponPosCurr = new Vector3[weaponPartsCount];
        weaponPosPrev = new Vector3[weaponPartsCount];
        weaponVelocity = new Vector3[weaponPartsCount];

        weaponPartHasSwung = new bool[weaponPartsCount];

        weaponParts = new Transform[weaponPartsCount];

        for (int i = 0; i < weaponPartsCount; i++)
        {
            weaponParts[i] =  weaponPartsParent.transform.GetChild(i).transform;
            weaponPartHasSwung[i] = false;
        }

        player = Networking.LocalPlayer;

        sandbagScript = sandbag.GetComponent<Sandbag>();
    }

    public override void OnPickup()
    {
        isHeld = true;
    }

    public override void OnDrop()
    {
        isHeld = false;
    }

    private void FixedUpdate()
    {
        RaycastHit hit;

        weaponTransformPosCurrent = weapon.position;
        weaponTransformVelocity = (weaponTransformPosCurrent - weaponTransformPosPrevious) / Time.fixedDeltaTime;

        for (int i = 0; i < weaponParts.Length; i++)
        {
            //weaponPosCurr[i] = weaponPartsParent.transform.GetChild(i).transform.position;
            weaponPosCurr[i] = weaponParts[i].position;
            weaponVelocity[i] = (weaponPosCurr[i] - weaponPosPrev[i]) / Time.fixedDeltaTime;

            // Only do RayCasts whilst bat is being held
            // Need to look up how to make this section neater as I have heard multiple nested ifs is bad practise
            if (isHeld)
            {
                if (!weaponPartHasSwung[i])
                {
                    if (Physics.Raycast(weaponPosPrev[i], (weaponPosCurr[i] - weaponPosPrev[i]), out hit, (weaponPosCurr[i] - weaponPosPrev[i]).magnitude, layerMask))
                    {
                        // Find the difference between hit.point and batPosCurr
                        var hitDiff = weaponPosCurr[i] - hit.point;
                        // Normalize this distance to get a unit direciton
                        var hitDiffNormalized = hitDiff.normalized;
                        // Find the new difference of a second point, which is the ideal transform point
                        // This point is the hit distance minus a fraction of hitDiffNormalized, which is a unit line drawn from hitPosCurr to hit.point
                        // Subtracting a fraction of this hitDiffNormalized variable creates a Vector3 position similar to the hit.position but it is slightly outside the sandbag
                        var idealTransformPoint = hit.point - (hitDiffNormalized * 0.05f);
                        // Then calculate this new difference in distance between the bat part that hit and the idealTransformPoint
                        var newDiff = weaponPosCurr[i] - idealTransformPoint;

                        // Calculate a factor of the ratio between how fast the bat is moving versus how fast the bat part that makes the collision is moving
                        // If the bat part is moving faster than the bat (which it will under normal circumstances) then the factor will be less than 1
                        // This factor is used to move the batTransform by a suitable amount based on how fast the bat part is moving
                        // This calculation should let the code work with any bat part along the bat, so long as I have the velocity of that part.
                        var inverseFactor = (weaponTransformVelocity.magnitude / weaponVelocity[0].magnitude);

                        
                        if (yetAnotherBool == false && weaponType == 0)
                        {
                            batShakeScript.SetProgramVariable("start", true);
                            yetAnotherBool = true;

                            baseballBatGhost.transform.position = weapon.transform.position - (newDiff * inverseFactor);
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
                            damagetext = Instantiate(floatingTextPrefab, sandbag.transform);
                            damagetext.transform.position = sandbag.transform.position + new Vector3(0.0f, 1.5f, 0.0f);
                            damagetext.transform.GetChild(0).GetComponent<Animator>().Play("TextFloatAnimation", 0, 0.0f);
                        }
                        else
                        {
                            damagetext.transform.GetChild(0).GetComponent<Animator>().Play("TextFloatAnimation", 0, 0.0f);
                        }

                        weaponPartHasSwung[i] = true;

                        // If we are here, that means the weapon has made contact with the sandbag (presumably)
                        // The local player needs to be the owner of the Sandbag to update the networked variables, so make them the owner
                        if (Networking.GetOwner(sandbag.gameObject) != Networking.LocalPlayer)
                        {
                            Networking.SetOwner(Networking.LocalPlayer, sandbag.gameObject);
                        }

                        // Invert the collision normal so it points to the centre of the Sandbag (roughly to the centre of mass)
                        // Maybe could redo this section so the normal points towards the centre of mass, because of now it is just a 'normal' drawn away from the hit location
                        // To expand on this, the line is drawn perpendicular to the mesh hit point, rather than towards the centre of mass, which I think might be more useful for this physics simulation
                        // As this could possibly be used to calculate an angular rotation to apply at the time of launch
                        Vector3 hitNormalInverted = hit.normal * -1;

                        // Use Phythagoras theorem to calculate the unknown third side of a triangle
                        // The triangle is a 2d triangle using the x and z components of the inverted hit normal and bat velocity
                        float triangleSideA = Mathf.Sqrt((hitNormalInverted.x * hitNormalInverted.x) + (hitNormalInverted.z * hitNormalInverted.z));
                        float triangleSideB = Mathf.Sqrt((weaponVelocity[i].x * weaponVelocity[i].x) + (weaponVelocity[i].z * weaponVelocity[i].z));
                        float triangleSideC = Mathf.Sqrt((Mathf.Pow((weaponVelocity[i].x - hitNormalInverted.x), 2)) + (Mathf.Pow((weaponVelocity[i].z - hitNormalInverted.z), 2)));

                        // Create the two sides of the equation to make it easier to see the equation
                        float topOfAngleEquation = (triangleSideA * triangleSideA) + (triangleSideB * triangleSideB) - (triangleSideC * triangleSideC);
                        float bottomOfAngleEquation = (2 * triangleSideA * triangleSideB);

                        // Find the angle between the normal and velocity '2D' vectors
                        float angleBetweenNormalAndVelocity = Mathf.Acos(topOfAngleEquation / bottomOfAngleEquation);

                        // Use the angle to determine how much of the velocity should be used
                        resultantImpulse.x = hitNormalInverted.x * (triangleSideB * Mathf.Cos(angleBetweenNormalAndVelocity));
                        resultantImpulse.y = weaponVelocity[i].y * Mathf.Cos(angleBetweenNormalAndVelocity);
                        resultantImpulse.z = hitNormalInverted.z * (triangleSideB * Mathf.Cos(angleBetweenNormalAndVelocity));

                        // Roll for crit
                        int critRoll =  UnityEngine.Random.Range(1, 101);

                        if (critRoll <= critChance)
                        {
                            sandbagScript.SetProgramVariable("storedMomentum", (resultantImpulse * (m_ass / (float)weaponParts.Length)) * 2);
                            recentDamage += ((resultantImpulse * (m_ass / (float)weaponParts.Length)) * 2).magnitude;
                            damagetext.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = recentDamage.ToString("0.00");
                        }
                        else
                        {
                            sandbagScript.SetProgramVariable("storedMomentum", (resultantImpulse * (m_ass / (float)weaponParts.Length)) * 2);
                            recentDamage += ((resultantImpulse * (m_ass / (float)weaponParts.Length))).magnitude;
                            damagetext.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = recentDamage.ToString("0.00");
                        }
                    }
                }
            }
            weaponPosPrev[i] = weaponPosCurr[i];
        }
        weaponTransformPosPrevious = weaponTransformPosCurrent;
    }

    private void Update()
    {
        // If the weapon is a bat
        if (weaponType == 0)
        {
            if (!yetAnotherBool)
            {
                baseballBatGhost.transform.position = weapon.transform.position;
                baseballBatGhost.transform.rotation = weapon.transform.rotation;
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
        }
    }
}
