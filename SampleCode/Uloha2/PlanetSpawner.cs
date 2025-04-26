using UnityEngine;

public class PlanetSpawner : MonoBehaviour
{
    private float minDistance = 0.5f;
    private float distanceStep = 0.3f;

    public void CreatePlanet(int index)
    {
        float distance = minDistance +  index * distanceStep;
        Vector3 position = new Vector3(distance, 0, 0);

        GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        planet.name = "Planet_" + index;
        planet.transform.position = position;        
        planet.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        planet.tag = "Planet";

        // Pridaj komponenty automaticky
        planat.AddComponent<PlanetBehavior>();
        planet.AddComponent<ColorChanger>();
        planet.AddComponent<PlanetCollision>();        
    }
}
