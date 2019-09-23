using UnityEngine;
using System.Collections;

public static class MathsHelper  {

	public static Vector2 rotateVector(Vector2 vec, float angleInDegrees) {
		
		float radians = angleInDegrees * Mathf.Deg2Rad;
		
		return new Vector2(Mathf.Cos(radians) * vec.x - Mathf.Sin(radians) * vec.y,
		                   Mathf.Sin(radians) * vec.x + Mathf.Cos(radians) * vec.y);
		
	}
}
