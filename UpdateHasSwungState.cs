
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class UpdateHasSwungState : UdonSharpBehaviour
{
    public WeaponScript[] WeaponScripts;

    public void SwingStateReset()
    {
        for (int i = 0; i < WeaponScripts.Length; i++)
        {
            WeaponScripts[i].SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResetSwing");
        }
    }

    public void SetSwingTrue()
    {
        for (int i = 0; i < WeaponScripts.Length; i++)
        {
            WeaponScripts[i].SendCustomEvent("SetSwingTrue");
        }
    }
}
