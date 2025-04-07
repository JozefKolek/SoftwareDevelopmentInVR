using UnityEngine;

public class SystemManager : MonoBehaviour
{
    private PlanetSpawner spawner;

    public int initialPlanets = 8;
    public int maxPlanets = 20;
    private int createdPlanets = 0;

    void Start()
    {
        spawner = gameObject.AddComponent<PlanetSpawner>();

        for (int i = 0; i < initialPlanets; i++)
        {
            spawner.CreatePlanet(i);
            createdPlanets++;
        }
        SetRandomFunctions();
    }

    public void SetRandomFunctions()
    {
        var planets = GameObject.FindGameObjectsWithTag("Planet");        
    }    
}
