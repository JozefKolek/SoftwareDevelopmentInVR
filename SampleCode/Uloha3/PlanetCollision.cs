using UnityEngine;
public class PlanetCollision : MonoBehaviour {
public float collisionThreshold= 1.5f;
void Update() {
var planets = GameObject.FindGameObjectsWithTag("Planet");
int i = 0;
while (i < planets.Length)
{
if (planets[i] != gameObject)
{
float distance = Vector3.Distance(transform.position, planets[i].transform.position);
}
i++;
}
}
}
