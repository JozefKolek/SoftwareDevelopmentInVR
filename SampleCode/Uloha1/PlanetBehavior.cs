using System;
using System.Collections.Generic;
using UnityEngine;
public class PlanetBehavior : MonoBehaviour {
public Transform sun;
public List<float> orbitSpeed = new List<float>{ 47f, 35f, 29f, 24f, 13f, 9f, 6f, 5f };
public float selfRotationSpeed= 50f;
private float angle= 0f;
private int orbitCount= 0;
void Start() {
if (sun == null)
{
sun = GameObject.FindWithTag("Sun")?.transform;
}
SetPositionToSun();
}
void Update() {
if (sun == null)
{
return;
}
int index = 0;
Int32.TryParse(name.Substring(name.Length - 1), out index);
transform.RotateAround(sun.position, Vector3.up, orbitSpeed[index] * Time.deltaTime);
angle += orbitSpeed[index] * Time.deltaTime;
if (angle >= 360f)
{
angle = 0f;
orbitCount++;
}
}
public void SetPositionToSun() {
if (sun == null)
{
return;
}
transform.position += sun.position;
}
}
