
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class BatShake : UdonSharpBehaviour
{
    // The approach to tackle this problem was suggested by this video from Thomas Friday
    // https://youtu.be/BQGTdRhGmE4?si=jengHx0rXt1PxFPM

    public bool start = false;
    public AnimationCurve curve;
    public float animDuration = 0.5f;

    // this should probably be Vector3.zero because it is a child to a parent, and the child transform pos is 0, 0, 0
    private Vector3 startPos = Vector3.zero;
    private float elapsedTime = 0.0f;

    private void Update()
    {
        if (start)
        {
            if (elapsedTime < animDuration)
            {
                elapsedTime += Time.deltaTime;
                float strength = curve.Evaluate(elapsedTime/animDuration);
                transform.localPosition = startPos + Random.insideUnitSphere * strength;
            }
            else
            {
                transform.localPosition = startPos;
                //transform.position = startPos;
                start = false;
                elapsedTime = 0.0f;
            }
        }
    }
}
