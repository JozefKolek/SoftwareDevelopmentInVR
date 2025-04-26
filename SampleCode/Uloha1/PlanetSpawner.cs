using UnityEngine;
public class PlanetSpawner : MonoBehaviour {
private float minDistance= 0.4f;
private double[] vzdialenostPomery = { 1,1.69,1.4,1.64,3.03,1.96,1.98,1.51};
public void CreatePlanet(int index) {
float distance = minDistance;
for(int i = 0; i < index; i++)
{
    distance *= (float) vzdialenostPomery[i];
}
Vector3 position = new Vector3(distance, 0, 0);
GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
planet.name = "Planet_" + index;
planet.transform.position = position;
planet.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
planet.tag = "Planet";
planet.AddComponent<PlanetBehavior>();
planet.AddComponent<ColorChanger>();
planet.AddComponent<PlanetCollision>();
}
}
