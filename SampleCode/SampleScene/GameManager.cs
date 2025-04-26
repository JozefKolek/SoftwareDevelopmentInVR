using UnityEngine;
public class GameManager : MonoBehaviour {
private ShapeMover shapeMover;
void Start() {
shapeMover = GetComponent<ShapeMover>();
if (shapeMover == null)
{
Debug.LogError("ShapeMover komponent nebol nájdený na tomto objekte!");
}
}
public void ChangeSquareSpeed(float newSpeed) {
if (shapeMover != null)
{
shapeMover.SetSquareSpeed(newSpeed);
}
}
public void ChangeCircleSpeed(float newSpeed) {
if (shapeMover != null)
{
shapeMover.SetCircleSpeed(newSpeed);
}
}
}
