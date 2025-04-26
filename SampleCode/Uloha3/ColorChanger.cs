using System;
using System.Collections.Generic;
using UnityEngine;
public class ColorChanger : MonoBehaviour {
public float changeInterval= 5f;
private float timer= 0f;
public Color color= Color.white;
private List<Color> zoznamColors = new List<Color>() { Color.gray };
private void Start()
{
zoznamColors.Add(new Color(1f, 0.8f, 0.1f));
zoznamColors.Add(Color.blue);
zoznamColors.Add(Color.red);
zoznamColors.Add(new Color(0.8f, 0.5f, 0.2f));
zoznamColors.Add(new Color(1f, 0.9f, 0.4f));
zoznamColors.Add(Color.cyan);
zoznamColors.Add(new Color(0.2f, 0.2f, 0.8f));
}
private void Update() {
Renderer renderer = GetComponent<Renderer>();
if (renderer != null)
{
int index = 0; 
Int32.TryParse(name.Substring(name.Length-1),out index);
renderer.material.color = zoznamColors[index];
}
}
}
