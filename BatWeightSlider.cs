
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class BatWeightSlider : UdonSharpBehaviour
{
    public TextMeshProUGUI costOfUpgrade;
    public TextMeshProUGUI debug;
    VRCPlayerApi player;
    Slider weightSlider;
    [UdonSynced] private float weightSliderValue;
    private float weightSliderPrevious;
    [UdonSynced, FieldChangeCallback(nameof(moneyCallback))] private float moneyFromOutside = 10.0f;
    [UdonSynced] private float moneyFromInside = 10.0f;
    [UdonSynced] private float upgradeCost = 10.0f;
    private bool didIChangeTheValue = false;

    void Start()
    {
        player = Networking.LocalPlayer;
        weightSlider = this.GetComponent<Slider>();
    }

    public void WeightIncrease()
    {
        if (moneyFromInside >= upgradeCost)
        {
            weightSlider.value += 0.1f;
            //moneyFromOutside -= upgradeCost;
            moneyFromInside -= upgradeCost;
            //moneyCallback = upgradeCost * -1;
            upgradeCost++;
            //UpdateCostOfUpgradeText();
        }
    }

    public void WeightDecrease()
    {
        weightSlider.value -= 0.1f;
    }

    public void UpdateCostOfUpgradeText()
    {
        costOfUpgrade.text = upgradeCost.ToString();
    }

    public float moneyCallback
    {
        set 
        {
            if (Networking.GetOwner(this.gameObject) != Networking.LocalPlayer)
            {
                return;
            }
            // using += for setting money value does not seem to work for Serialization
            // So we will need to do the maths inside of here and then serialize the value to other players
            //moneyFromOutside = value + moneyFromOutside;
            float temp = moneyFromInside + value;
            moneyFromInside = temp;
            RequestSerialization();
        }
        get { return moneyFromOutside; }
    }

    public void Update()
    {
        debug.text = moneyFromOutside.ToString() + "\n";
        debug.text += moneyFromInside.ToString() + "\n";
        debug.text += upgradeCost.ToString() + "\n";
        // This would mean the value has changed on the local player's side.
        // Which ever player experiences this becomes the owner of the slider for Serialization purposes.
        if (weightSlider.value != weightSliderPrevious)
        {
            Networking.SetOwner(player, this.gameObject);
            didIChangeTheValue = true;
            weightSliderValue = weightSlider.value;
        }
        if (Networking.GetOwner(this.gameObject) == player)
        {
            RequestSerialization();
            OnDeserialization();
        }
        weightSliderPrevious = weightSlider.value;
    }

    public override void OnDeserialization()
    {
        // Player's with this bool flag set to false will have the previous value set to current value so they do not run the code in the Update loop
        // This is my implementation of a system that is able to identify which player has interacted with the slider, as there is no Interact() event to take advantage of
        if (!didIChangeTheValue)
        {
            weightSlider.value = weightSliderValue;
            weightSliderPrevious = weightSlider.value;
        }
        didIChangeTheValue = false;
        UpdateCostOfUpgradeText();
    }

}
