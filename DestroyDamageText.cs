using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyDamageText : MonoBehaviour
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
