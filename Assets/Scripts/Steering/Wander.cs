using UnityEngine;
using System.Collections;

// Could make this more complex, but it seems fairly alright atm.
public class Wander : SteeringBehaviour
{
	public float weight;
	public float wanderingSpeed;
	public float updateFrequency;

	private float lastTimeDirectionChanged;
	private Vector2 currentDir = Vector2.zero;

	public override Vector2 GetSteering()
	{
		if ((Time.realtimeSinceStartup - lastTimeDirectionChanged) > updateFrequency) {
			lastTimeDirectionChanged = Time.realtimeSinceStartup;
			var newDir =  new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f)).normalized;
			currentDir = newDir * wanderingSpeed * weight;
		}

		return currentDir;
	}
}
