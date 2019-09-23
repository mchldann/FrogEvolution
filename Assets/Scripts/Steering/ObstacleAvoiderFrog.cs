using UnityEngine;
using System.Collections;

public class ObstacleAvoiderFrog : MonoBehaviour
{
	private CircleCollider2D circleCollider;
	private BoxCollider2D boxCollider;
	private float lowerTerminal = -1.0f;
	private float upperTerminal = 1.0f;
	private SteeringBehaviour[] steeringBehaviours;
	private float initialWanderWeight = 0.0f;
	private float initialAlignmentWeight = 0.0f;
	private float initialCohesionWeight = 0.0f;
	private float initialSeperationWeight = 0.0f;
	
	// Despite all our efforts, the flies can still get stuck sometimes...
	// Turn wander/flocking back on when this happens to try to get unstuck
	public bool isStuck = false;
	private float timeStuck = 0.0f;
	private float confirmStuckTime = 0.5f;
	private Vector2 testPosition = Vector2.zero;
	private float stuckRadius = 0.1f;
	public float kickForce = 50.0f;
	private Collider2D lastColliderTouched = null;
	private float conservativeMultiplier = 1.1f;
	
	public float detectionDist = 2.0f;
	public float anglePrecision = 10.0f;
	public float speedAdjustment = 0.5f;
	public bool drawDebug;
	public string[] obstacleLayerNames;
	
	void Awake()
	{
		steeringBehaviours = GetComponents<SteeringBehaviour>();

		// Get physical colliders (not triggers)
		CircleCollider2D[] circleColliders = GetComponents<CircleCollider2D>();
		foreach (CircleCollider2D cc in circleColliders) {
			if (!cc.isTrigger) {
				circleCollider = cc;
				break;
			}
		}

		BoxCollider2D[] boxColliders = GetComponents<BoxCollider2D>();
		foreach (BoxCollider2D bc in boxColliders) {
			if (!bc.isTrigger) {
				boxCollider = bc;
				break;
			}
		}
	}
	
	void Update() {
		
		if (((Vector2)(transform.position) - testPosition).magnitude < stuckRadius) {
			
			timeStuck += Time.deltaTime;
			
			if (timeStuck > confirmStuckTime) {
				isStuck = true;
			}
			
		} else {
			testPosition = (Vector2)(transform.position);
			timeStuck = 0.0f;
			isStuck = false;
		}
	}
	
	void OnTriggerEnter2D(Collider2D other) {
		lastColliderTouched = other;
	}
	
	void OnTriggerStay2D(Collider2D other) {
		lastColliderTouched = other;
	}
	
	void OnTriggerExit2D(Collider2D other) {
		if (lastColliderTouched == other) {
			lastColliderTouched = null;
		}
	}
	
	public Vector2 AvoidObstacles(Vector2 steering) {
		
		bool freePathFound = false;
		Vector2 adjustedSteering = Vector2.zero;
		
		int layerMask = 0;
		
		// Kick the object back to hopefully unstick it!
		if (isStuck) {
			
			if (lastColliderTouched != null) {
				GetComponent<Rigidbody2D>().AddForce(-kickForce * (lastColliderTouched.transform.position - transform.position).normalized);
			}
		}
		
		foreach (string layerName in obstacleLayerNames) {
			layerMask = layerMask | (1 << LayerMask.NameToLayer(layerName));
		}
		
		// Calculate the four corners of the box collider.
		Vector2[] boxPoints = new Vector2[4];

		if (circleCollider != null) {
			boxPoints[0] = circleCollider.offset + conservativeMultiplier * new Vector2(circleCollider.radius, circleCollider.radius);
			boxPoints[1] = circleCollider.offset + conservativeMultiplier * new Vector2(-circleCollider.radius, -circleCollider.radius);
			boxPoints[2] = circleCollider.offset + conservativeMultiplier * new Vector2(circleCollider.radius, -circleCollider.radius);
			boxPoints[3] = circleCollider.offset + conservativeMultiplier * new Vector2(-circleCollider.radius, circleCollider.radius);
		}

		if (boxCollider != null) {
			boxPoints[0] = boxCollider.offset + conservativeMultiplier * boxCollider.size / 2.0f;
			boxPoints[1] = boxCollider.offset - conservativeMultiplier * boxCollider.size / 2.0f;
			boxPoints[2] = boxCollider.offset + conservativeMultiplier * new Vector2(boxCollider.size.x, -boxCollider.size.y) / 2.0f;
			boxPoints[3] = boxCollider.offset + conservativeMultiplier * new Vector2(-boxCollider.size.x, boxCollider.size.y) / 2.0f;
		}

		// Try finding a path three times:
		// - The first time we'll try a conservative approach with a hitbox larger than the actual collider
		// - Only the second attempt we'll try fitting the actual collider
		// - If the previous attempts failed (hopefully rare!) then we'll just try a single raycast on the
		//   assumption that there may be a narrow gap we can fit through.
		for (int attempt = 0; attempt <= 2; attempt++) {

			// Relax the condition of fitting the full collider through if we couldn't find a path the first time
			if (attempt == 1) {
				for (int i = 0; i < 4; i++) {
					boxPoints[i] /= conservativeMultiplier;
				}
			}
			else if (attempt == 2) {
				boxPoints = new Vector2[1];
				boxPoints[0] = Vector2.zero;
			}
			
			// Try twisting angles up to 100 degrees, taking the first free path available. We don't want to travel
			// backwards (hence why the upper terminal is not 180 degrees) since that could get us stuck in an infinite
			// loop, going backwards and forwards once we hit a wall. With a maximum angle of 90 degrees, the flies can
			// still *just* get stuck sometimes, but at 100 degrees they seem fine. (Admittedly this is a bit of a hack.)
			for (float angle = 0.0f; angle <= 150.0f; angle += anglePrecision) {
				
				// If sign == -1, we're trying to twist right. If sign == 1, we're trying to twist left.
				float sign;
				for (sign = lowerTerminal; sign <= upperTerminal; sign += 2.0f) {
					
					bool obstacle = false;
					
					// Try raycasting to find a path we can fit through
					Vector2 rayCastVec = MathsHelper.rotateVector(steering, sign * angle).normalized;
					
					foreach (Vector2 boxPoint in boxPoints) {
						
						RaycastHit2D[] rayHits = Physics2D.RaycastAll((Vector2)(transform.position) + boxPoint, rayCastVec, detectionDist, layerMask);
						
						foreach (var rayHit in rayHits)
						{
							// As the ray includes the object itself skip it.
							if(rayHit.collider.gameObject.name == gameObject.name)
								continue;
							
							obstacle = true;
							break;
						}
						
						if (obstacle) {
							break;
						}
					}
					
					if (obstacle) {
						
						// Turning "drawDebug" on will give you a pretty good idea of how the logic works
						if (drawDebug) {
							Debug.DrawLine((Vector2)(transform.position), (Vector2)(transform.position) + rayCastVec, Color.red);
						}
						
					} else {
						
						if (drawDebug) {
							Debug.DrawLine((Vector2)(transform.position), (Vector2)(transform.position) + rayCastVec, Color.green);
						}
						
						// We found a free path along this direction.
						// Ensure that the steering magnitude is unchanged.
						adjustedSteering = rayCastVec * steering.magnitude;
						
						// If there's nothing in the way we can try moving left and right next time.
						if (angle == 0.0f) {
							lowerTerminal = -1.0f;
							upperTerminal = 1.0f;
							
							// If we just moved right to avoid an obstacle, don't try left on the next Update() or we'll oscillate!
						} else if (sign == lowerTerminal) {
							upperTerminal = lowerTerminal;
							
							// If we just moved left to avoid an obstacle, don't try right next time.
						} else {
							lowerTerminal = upperTerminal;
						}
						
						// Don't wander or flock if we're avoiding an obstacle
						float wanderWeight = initialWanderWeight;
						float alignmentWeight = initialAlignmentWeight;
						float cohesionWeight = initialCohesionWeight;
						float seperationWeight = initialSeperationWeight;
						
						if (angle != 0.0f) {
							wanderWeight = 0.0f;
							alignmentWeight = 0.0f;
							cohesionWeight = 0.0f;
							seperationWeight = 0.0f;
							
							// Since wandering and flocking tend to slow the flies somewhat, we need to adjust their
							// speed a bit when wandering/flocking are disabled so that their movement looks natural.
							adjustedSteering *= speedAdjustment;
						}
						
						foreach (SteeringBehaviour steeringBehaviour in steeringBehaviours) {
							
							if (steeringBehaviour.GetType() == typeof(Wander)) {
								((Wander)steeringBehaviour).weight = wanderWeight;
							}
							
							if (steeringBehaviour.GetType() == typeof(Flocking)) {
								((Flocking)steeringBehaviour).alignmentWeight = alignmentWeight;
								((Flocking)steeringBehaviour).cohesionWeight = cohesionWeight;
								((Flocking)steeringBehaviour).seperationWeight = seperationWeight;
							}
						}
						
						freePathFound = true;
						break;
					}
				}
				
				if (freePathFound) {
					break;
				}
			}
			
			if (freePathFound) {
				break;
			}
		}
		
		// If we hit a dead end by going right then try going left (and vice-versa).
		if (!freePathFound) {
			if (lowerTerminal == 1.0f) {
				lowerTerminal = -1.0f;
				upperTerminal = -1.0f;
			} else if (upperTerminal == -1.0f) {
				lowerTerminal = 1.0f;
				upperTerminal = 1.0f;
			}
		}
		
		// If there's nothing in the way (i.e. angle == 0), then adjustedSteering will be equal to steering.
		return adjustedSteering;
	}
}
