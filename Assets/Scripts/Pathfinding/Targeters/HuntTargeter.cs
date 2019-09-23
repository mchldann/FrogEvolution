using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
[RequireComponent(typeof(Flocking))]
public class HuntTargeter : Targeter {

	public GameObject Target;
	public bool dumbAttack = false;

	private Flocking flocker;

	void Awake() {
		flocker = GetComponent<Flocking>();
	}

	public override Vector2? GetTarget ()
	{
		// Default
		flocker.seperationWeight = 0.2f;

		if (Target == null) {
			return null;
		}

		// We can't chase the frog if it's underwater
		if ((Target != null) && (Target.tag == "Player") && Target.GetComponent<PlayerInfo>().IsUnderwater()) {
			return null;
		}
		
		// Player velocity
		Vector2 normPlayerVelocity = Target.GetComponent<Rigidbody2D>().velocity;
		
		// All attack if the player is still
		if (dumbAttack || (normPlayerVelocity == Vector2.zero)) {
			return (Vector2?)(Target.transform.position);
		}
		
		normPlayerVelocity.Normalize();
		
		// Vector to player
		Vector2 posDifference = (Vector2)(Target.transform.position - transform.position);

		// Component of vector to player that's in the direction of the player's velocity.
		// If it's positive then we're chasing, otherwise we're coming head-on
		float compAlongVelocity = Vector2.Dot(posDifference, normPlayerVelocity);

		Vector2 vecB = compAlongVelocity * normPlayerVelocity;
		Vector2 vecA = posDifference - vecB;

		float snakeSpeed = this.GetComponent<Movement>().speed;
		float targetSpeed = Target.GetComponent<Rigidbody2D>().velocity.magnitude;

		//Debug.Log ("Snake speed " + snakeSpeed + ", Target speed " + targetSpeed);

		float a = (snakeSpeed * snakeSpeed) / (targetSpeed * targetSpeed) - 1.0f;
		float b = 2.0f * vecB.magnitude;

		// Adjustment to formula if chasing
		if (compAlongVelocity > 0) {
			b *= -1.0f;
		}

		float c = -(vecA.sqrMagnitude + vecB.sqrMagnitude);

		float discriminant = b * b - 4.0f * a * c;

		if (discriminant < 0) {

			// Flank target
			if (compAlongVelocity < 0) {
				return (Vector2?)(transform.position) - (Vector2?)vecB;
			} else {
				return (Vector2?)(transform.position) + (Vector2?)vecB;
			}

		} else {
			float solution1 = (-b + Mathf.Sqrt(discriminant)) / (2.0f * a);
			float solution2 = (-b - Mathf.Sqrt(discriminant)) / (2.0f * a);

			float solution = float.MaxValue;

			if (solution1 > 0) {
				solution = solution1;
			}

			if ((solution2 > 0) && (solution2 < solution)) {
				solution = solution2;
			}

			// No positive solutions
			if (solution == float.MaxValue) {
				// Flank target
				if (compAlongVelocity < 0) {
					return (Vector2?)(transform.position) - (Vector2?)vecB;
				} else {
					return (Vector2?)(transform.position) + (Vector2?)vecB;
				}
			}

			return (Vector2?)(Target.transform.position) + solution * (Vector2?)normPlayerVelocity;
		}
	}
}
