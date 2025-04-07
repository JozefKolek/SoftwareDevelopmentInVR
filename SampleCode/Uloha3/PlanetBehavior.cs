using UnityEngine;

public class PlanetBehavior : MonoBehaviour
{
    public Transform sun;
    public float orbitSpeed = 10f;
    public float selfRotationSpeed = 50f;
    private float angle = 0f;
    private int orbitCount = 0;

    void Start()
    {
        if (sun == null)
            sun = GameObject.FindWithTag("Sun")?.transform;
        SetPositionToSun();
    }

    void Update()
    {
        if (sun == null) return;

        transform.RotateAround(sun.position, Vector3.up, orbitSpeed * Time.deltaTime);

        angle += orbitSpeed * Time.deltaTime;
        if (angle >= 360f)
        {
            angle = 0f;
            orbitCount++;            
        }
    }

    public void SetPositionToSun()
    {
        if (sun == null) {
            return; 
        }
        transform.position += sun.position;
    }
}
