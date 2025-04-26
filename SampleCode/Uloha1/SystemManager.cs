using UnityEngine;
public class SystemManager : MonoBehaviour {
private PlanetSpawner spawner;
public int initialPlanets= 8;
public int maxPlanets= 20;
private int createdPlanets= 0;
void Start() {
spawner = gameObject.AddComponent<PlanetSpawner>();
int i = 0;
spawner.CreatePlanet(i);
createdPlanets++;
i++;
SetRandomFunctions();
}
public void SetRandomFunctions() {
var planets = GameObject.FindGameObjectsWithTag("Planet");
}
}
