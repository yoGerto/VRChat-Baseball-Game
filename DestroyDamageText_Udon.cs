
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DestroyDamageText_Udon : UdonSharpBehaviour
{
    // Code used here is from the Youtube Tutorial 'How to Make Damage Text and Number Popups in Unity | Tutorial' by Wintermute Digital
    // https://www.youtube.com/watch?v=LjNsDVYXfrk

    public void DestroyParent()
    {
        Debug.Log("Destroying");
        GameObject parentGameObject = gameObject.transform.parent.gameObject;
        Destroy(parentGameObject);
    }
}
    