using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// TODO When the flock reaches a border they should move away from it 
// to stay within the world boundaries.
[RequireComponent(typeof(Rigidbody2D))]
public class Flocking : SteeringBehaviour 
{
	private const string BOUNDARIES = "boundaries";
	public float alignmentWeight = 0.1f;
	public float cohesionWeight = 0.1f;
	public float seperationWeight = 0.1f;
	public float neighbourDist = 30;
	private Movement movement;
	//private static List<GameObject> agents;
	private static Hashtable flocks = new Hashtable();

	private delegate Vector2 ReturnVector(GameObject agent);
	private delegate Vector2 Finalization(Vector2 velocity, uint neighbourCount);

	public static void DestroyFlockMember(GameObject flockMember) 
	{
		if (flockMember != null) {
			((Hashtable)flocks[flockMember.tag]).Remove(flockMember);
		}
	}

	private void EnsureFlocksOK() {

		// Store a list of members for each type of flock (flies and snakes)
		if (!(flocks.ContainsKey(this.tag))) {
			flocks.Add(this.tag, new Hashtable());
		}

		if (!((Hashtable)flocks[this.tag]).Contains(gameObject)) {
			((Hashtable)flocks[this.tag]).Add(gameObject, gameObject);
		}
	}

	public override Vector2 GetSteering()
	{
		// Compute the new velocity, taking into consideration the weights of each behaviour.
		Vector2 vel = (computeAlignment() * alignmentWeight) + (computeCohesion() * cohesionWeight) + (computeSeperation() * seperationWeight);
		vel.Normalize();

		// Moved this line here because occasionally Start() wasn't setting it correctly and it was throwing an exception
		movement = GetComponent<Movement>();

		vel *= movement.speed;
		
		return vel;
	}


	// Computation to add to the velocity for the "Alignment" behaviour.
	private Vector2 alignmentVector(GameObject agent)
	{
		return agent.GetComponent<Rigidbody2D>().velocity;
	}

	
	// Final steps for the "Alignment" behaviour.
	private Vector2 alignmentFinalize(Vector2 velocity, uint neighbourCount)
	{
		velocity /= (float)neighbourCount;
		velocity.Normalize();
		
		return velocity;
	}


	// Computation to add to the velocity for the "Cohesion" behaviour.
	private Vector2 cohesionVector(GameObject agent)
	{
		return agent.GetComponent<Rigidbody2D>().position;
	}


	// Final steps for the "Cohesion" behaviour.
	private Vector2 cohesionFinalize(Vector2 velocity, uint neighbourCount)
	{
		velocity /= (float)neighbourCount;
		Vector2 vectorToMass = velocity - new Vector2(transform.position.x, transform.position.y);
		vectorToMass.Normalize();
		
		return vectorToMass;
	}


	// Computation to add to the velocity for the "Seperation" behaviour.
	private Vector2 seperationVector(GameObject agent)
	{
		return agent.GetComponent<Rigidbody2D>().position - new Vector2(transform.position.x, transform.position.y);
	}


	// Final steps for the "Seperation" behaviour.
	private Vector2 seperationFinalize(Vector2 velocity, uint neighbourCount)
	{
		velocity /= (float)neighbourCount;
		velocity *= -1;
		velocity.Normalize();
		
		return velocity;
	}


	// Main algorithmic 'formula' of alignment, cohesion and seperation.
	// The passed functions handle the minute differences.
	private Vector2 runAlgorithm(ReturnVector vecFunc, Finalization finalizeFunc)
	{
		Vector2 velocity = Vector2.zero;
		uint neighbourCount = 0;

		// Ugh... Some super-annoying bugs can occur with the agent list when the frog eats a fly or the game is restarted.
		// This check as well as "staleAgents" below means that we should avoid any null reference crap.
		EnsureFlocksOK();

		Hashtable agents = (Hashtable)flocks[this.tag];

		List<GameObject> staleAgents = new List<GameObject>();

		foreach (object o in agents.Keys)
		{
			GameObject agent = (GameObject)o;

			if (agent == null) {
				staleAgents.Add(agent);
				continue;
			}

			if (agent == gameObject)
				continue;

			// Find neighbours of our agent to include in the calculation. 
			if ( Vector2.Distance(agent.transform.position, transform.position) < neighbourDist )
			{
				//Debug.Log("Found neighbour");
				velocity += vecFunc(agent);
				neighbourCount += 1;
			}
		}

		foreach (GameObject staleAgent in staleAgents) {
			agents.Remove(staleAgent);
		}
		
		if (neighbourCount == 0)
			return velocity;
		
		return finalizeFunc(velocity, neighbourCount);
	}


	// Run the main algorithm performing the operations
	// required of the "Alignment" behaviour.
	private Vector2 computeAlignment()
	{
		ReturnVector vecFunc = alignmentVector;
		Finalization finalizeFunc = alignmentFinalize;

		return runAlgorithm(vecFunc, finalizeFunc);
	}


	// Run the main algorithm performing the operations
	// required of the "Cohesion" behaviour.
	private Vector2 computeCohesion()
	{
		ReturnVector vecFunc = cohesionVector;
		Finalization finalizeFunc = cohesionFinalize;
		
		return runAlgorithm(vecFunc, finalizeFunc);
	}


	// Run the main algorithm performing the operations
	// required of the "Seperation" behaviour.
	private Vector2 computeSeperation()
	{
		ReturnVector vecFunc = seperationVector;
		Finalization finalizeFunc = seperationFinalize;
		
		return runAlgorithm(vecFunc, finalizeFunc);
	}
}
