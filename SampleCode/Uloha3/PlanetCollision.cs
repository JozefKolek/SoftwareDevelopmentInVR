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

    void Update()
    {
        var planets = GameObject.FindGameObjectsWithTag("Planet");

        for (int i = 0; i < planets.Length; i++)
        {
            if (planets[i] != gameObject)
            {
                float distance = transform.position.x -  planets[i].transform.position.x;
                if (distance < collisionThreshold && !isDodging[i])
                {
                    isDodging[i] = true;
                    Debug.Log($"{gameObject.name} uhýba pred {planets[i].name}!");
                    transform.position += Vector3.down * dodgeDistance;
                } else if (isDodging[i] && distance >= collisionThreshold) 
                {
                    transform.position -= Vector3.down * dodgeDistance;
                    isDodging[i] = false;
                }
            }
        }
    }    
}

