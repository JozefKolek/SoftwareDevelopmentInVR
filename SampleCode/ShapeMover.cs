using UnityEngine;

public class ShapeMover : MonoBehaviour
{
    private GameObject square;
    private GameObject circle;

    public float squareSpeed = 1f;  // Rýchlosť pohybu štvorca
    public float circleSpeed = 1f;  // Rýchlosť pohybu kruhu

    private Vector3 squareStartPosition;
    private Vector3 circleStartPosition;

    private float squareRotationAngle = 0f;
    private float circleRotationAngle = 0f;

    private void Start()
    {
        // Vytvorenie štvorca
        square = GameObject.CreatePrimitive(PrimitiveType.Cube);
        square.GetComponent<Renderer>().material.color = Color.red;  // Nastavenie farby štvorca
        square.transform.position = new Vector3(-3, 0, 0);  // Nastavenie počiatočnej pozície
        squareStartPosition = square.transform.position;

        // Vytvorenie kruhu
        circle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        circle.GetComponent<Renderer>().material.color = Color.blue;  // Nastavenie farby kruhu
        circle.transform.position = new Vector3(3, 0, 0);  // Nastavenie počiatočnej pozície
        circleStartPosition = circle.transform.position;
    }

    private void Update()
    {
        // Pohyb štvorca v štvorci
        squareRotationAngle += squareSpeed * Time.deltaTime;
        square.transform.position = squareStartPosition + new Vector3(Mathf.Sin(squareRotationAngle) * 5, 0, Mathf.Cos(squareRotationAngle) * 5);

        // Pohyb kruhu v kruhu
        circleRotationAngle += circleSpeed * Time.deltaTime;
        circle.transform.position = circleStartPosition + new Vector3(Mathf.Sin(circleRotationAngle) * 5, 0, Mathf.Cos(circleRotationAngle) * 5);
    }

    // Metódy na nastavenie rýchlosti počas behu
    public void SetSquareSpeed(float newSpeed)
    {
        squareSpeed = newSpeed;
    }

    public void SetCircleSpeed(float newSpeed)
    {
        circleSpeed = newSpeed;
    }
}
