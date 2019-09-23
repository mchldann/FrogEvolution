using UnityEngine;
using System.Collections;

// The main class which combines the other steering behaviour
// components together.
[RequireComponent(typeof(Movement))]
[RequireComponent(typeof(BoxCollider2D))]
public class SteeringController : MonoBehaviour
{
	private SteeringBehaviour[] steeringBehaviours;
	private Movement movement;
	private ObstacleAvoider avoider;
	private ObstacleAvoiderFrog avoiderFrog;

	public bool avoidObstacles;
	
	protected void Awake()
	{
		steeringBehaviours = GetComponents<SteeringBehaviour>();
		movement = GetComponent<Movement>();
		avoider = GetComponent<ObstacleAvoider>();
		avoiderFrog = GetComponent<ObstacleAvoiderFrog>();
	}

	protected void Update()
	{

		Vector2 steering = Vector2.zero;

		foreach (var steeringBehaviour in steeringBehaviours)
			steering += steeringBehaviour.GetSteering();

		// TO DO: Make this properly polymorphic (i.e. make a parent "ObstacleAvoider" class with subclasses for fly and frog)
		if (avoidObstacles) {
			if (avoider != null) {
				steering = avoider.AvoidObstacles(steering);
			}
			if (avoiderFrog != null) {
				steering = avoiderFrog.AvoidObstacles(steering);
			}
		}

		movement.Move(steering);
	}
}
