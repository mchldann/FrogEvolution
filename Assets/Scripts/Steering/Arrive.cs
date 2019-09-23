using UnityEngine;
using System.Collections;
/*
public class ArriveSteering : Steering 
{
	private float maxSpeed = 0.8f;
	private float slowRadius = 20f; // should slow at this point
	private float targetRadius = 2f; // can stop now
	private float timeToTarget = 0.25f; // the time in which we should achieve the target speed.

	public ArriveSteering(Vector3 character, Vector3 target):
		base(character, target)
	{
	}
	
	public override SteeringOutput getSteering()
	{
		var steering = new SteeringOutput();

		steering.linearVel = target - character;
		float distance = steering.linearVel.magnitude;

		// Test if we have arrived at a threshold within our target.
		if (distance < targetRadius)
		{
			Debug.Log("Arrived");
			steering.ignore = true;
			return steering;
		}

		// Normalize target velocity and get target speed.
		steering.linearVel.Normalize();
		// We are outside the slow radius, therefore we should
		// move as fast as we can!
		if (distance > slowRadius)
		{
			steering.linearVel *= maxSpeed;
		}
		else
		{
			Debug.Log("Slowing...");
			// The shorter the distance, the less speed we have.
			steering.linearVel *= maxSpeed * distance / slowRadius; 
		}

		// Get distance to target velocity and scale it
		// by the time in which we wish to achieve target speed.
		steering.linearVel = steering.linearVel - character;
		Debug.Log(steering.linearVel.ToString());
		steering.linearVel /= timeToTarget;
		Debug.Log(steering.linearVel.magnitude);

		// Clip acceleration to the maximum
		if (steering.linearVel.magnitude > maxAcceleration)
		{
			steering.linearVel.Normalize();
			steering.linearVel *= maxAcceleration;
		}

		// As we are working in 2D, ensure z is 0
		steering.linearVel.z = 0f;
		
		// The angular velocity will be handled by other
		// behaviours such as 'align'.
		steering.angularVel = 0f;

		steering.ignore = false;

		return steering;
	}
}*/
