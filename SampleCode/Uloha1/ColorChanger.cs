using UnityEngine;
public class ColorChanger : MonoBehaviour {
public float changeInterval= 5f;
private float timer= 0f;
public Color color= Color.white;
private void Update() {
Debug.Log("Farba ");
Renderer renderer = GetComponent<Renderer>();
if (renderer != null)
{
renderer.material.color = color;
}
}
}
