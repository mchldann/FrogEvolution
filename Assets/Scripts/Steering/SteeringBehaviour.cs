using UnityEngine;
using System.Collections;

// The class which should be attached to a gameobject.
// In inspector set the type of behaviour wanted as indicated
// via the BType enum.
[RequireComponent(typeof(Movement))]
public abstract class SteeringBehaviour : MonoBehaviour 
{
	public abstract Vector2 GetSteering();
}
