using UnityEngine;
public class ShapeMover : MonoBehaviour {
private GameObject square;
private GameObject circle;
public float squareSpeed= 1f;
public float circleSpeed= 1f;
private Vector3 squareStartPosition;
private Vector3 circleStartPosition;
private float squareRotationAngle= 0f;
private float circleRotationAngle= 0f;
private void Start() {
square = GameObject.CreatePrimitive(PrimitiveType.Cube);
square.GetComponent<Renderer>().material.color = Color.red;
square.transform.position = new Vector3(-3, 0, 0);
squareStartPosition = square.transform.position;
circle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
circle.GetComponent<Renderer>().material.color = Color.blue;
circle.transform.position = new Vector3(3, 0, 0);
circleStartPosition = circle.transform.position;
}
private void Update() {
squareRotationAngle += squareSpeed * Time.deltaTime;
square.transform.position = squareStartPosition + new Vector3(Mathf.Sin(squareRotationAngle) * 5, 0, Mathf.Cos(squareRotationAngle) * 5);
circleRotationAngle += circleSpeed * Time.deltaTime;
circle.transform.position = circleStartPosition + new Vector3(Mathf.Sin(circleRotationAngle) * 5, 0, Mathf.Cos(circleRotationAngle) * 5);
}
public void SetSquareSpeed(float newSpeed) {
squareSpeed = newSpeed;
}
public void SetCircleSpeed(float newSpeed) {
circleSpeed = newSpeed;
}
}
