using UnityEngine;

public class GameManager : MonoBehaviour
{
    private ShapeMover shapeMover;

    void Start()
    {
        // Nájdeme ShapeMover komponent v rovnakom objekte
        shapeMover = GetComponent<ShapeMover>();

        if (shapeMover == null)
        {
            Debug.LogError("ShapeMover komponent nebol nájdený na tomto objekte!");
        }
    }

    // Tieto metódy môžu meniť rýchlosť týchto objektov počas behu hry
    public void ChangeSquareSpeed(float newSpeed)
    {
        if (shapeMover != null)
        {
            shapeMover.SetSquareSpeed(newSpeed);
        }
    }

    public void ChangeCircleSpeed(float newSpeed)
    {
        if (shapeMover != null)
        {
            shapeMover.SetCircleSpeed(newSpeed);
        }
    }
}
