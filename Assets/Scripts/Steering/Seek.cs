using UnityEngine;
using System.Collections;

public struct SteeringOutput
{
	public Vector3 linearVel;
	public float angularVel;
	public bool ignore;
}

// Implementation of the seek and flee behaviours.
public class Seek : SteeringBehaviour
{
	private Movement move;
	private Vector2? target;

	public bool flee; // provide a switch in inspector window
	public float weight = 1f;
	public Targeter targeter;

	public void SetTargeter(Targeter targeter) {
		this.targeter = targeter;
	}

	protected void Awake()
	{
		move = GetComponent<Movement>();
	}


	public override Vector2 GetSteering()
	{
		Vector2 targetDir;

		target = targeter.GetTarget();

		if (target != null) {

			// Are we seeking or fleeing?
			if(!flee)
				targetDir = ((Vector2)target - (Vector2)(transform.position)).normalized;
			else
				targetDir = ((Vector2)(transform.position) - (Vector2)target).normalized;

			var targetVelocity = targetDir * move.acceleration * weight;

			return targetVelocity;

		} else {
			return Vector2.zero;
		}
	}
}
