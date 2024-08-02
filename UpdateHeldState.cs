
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class UpdateHeldState : UdonSharpBehaviour
{
    public Katana katanaScript;

    public override void OnPickup()
    {
        katanaScript.SetProgramVariable("isHeld", true);
    }

    public override void OnDrop()
    {
        katanaScript.SetProgramVariable("isHeld", false);
    }
}
