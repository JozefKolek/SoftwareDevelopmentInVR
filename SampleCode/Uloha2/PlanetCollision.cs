using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlanetCollision : MonoBehaviour
{
    public float collisionThreshold = 1.5f;
    public float dodgeDistance = 0.5f;
    public float returnDelay = 3f;

    private Vector3 originalPosition;
    private List<bool> isDodging = new List<bool>();
    private Coroutine returnCoroutine;

    void Start()
    {
        originalPosition = transform.position;
        var planets = GameObject.FindGameObjectsWithTag("Planet");
        foreach(var i in planets) { isDodging.Add(false); }
    }        
}