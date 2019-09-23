using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class NeuralNetSteering : SteeringBehaviour {
	
	public float updateFrequency = 0.2f;
	public ManagePen manager;
	public float minSteeringMag = 1.0f;
	public NeuralNet neuralNet;
	public GameObject selectedFly;
	public List<string> obstacleLayerNames;
	public float obstacleDistancePrecision = 0.1f;
	public float obstacleDetectionRange = 5.0f;
	public float InputFlickerPreventionFactor = 0.2f; // 0.0f means it will always choose the closest fly
	                                                  // A value of 0.2f means that the chosen fly won't change unless another fly is 20% closer
	public bool loadFromFile = false; 
	public string fileName;

	public float ShotReloadTime = 1.0f;
	public float ShotSpinTime = 0.3f;
	private float shotTimer = 999.0f;

	private float updateTimer = 0.0f;
	public List<float> netInput;
	public float[] netInputArr;
	private float[] obstacleInfo;
	private CircleCollider2D circleCollider;
	private Movement movement;
	private Mouth mouthScript;
	private PlayerInfo playerInfo;
	//private float conservativeMultiplier = 1.5f;

	void Awake()
	{
		movement = GetComponent<Movement>();
		mouthScript = GetComponentInChildren<Mouth>();
		playerInfo = GetComponent<PlayerInfo>();

		// Get the physical collider (not the trigger)
		CircleCollider2D[] circleColliders = GetComponents<CircleCollider2D>();
		foreach (CircleCollider2D cc in circleColliders) {
			if (!cc.isTrigger) {
				circleCollider = cc;
				break;
			}
		}

		if (loadFromFile)
			LoadNetFromFile();
	}
	
	public void Update() {

		shotTimer += Time.deltaTime;

		if (shotTimer > ShotSpinTime) {
			movement.StopOverrideRotation();
		}

		if (obstacleInfo != null) {
			Debug.DrawLine((Vector2)(transform.position), (Vector2)(transform.position) + new Vector2(obstacleInfo[0], 0.0f), Color.yellow);
			Debug.DrawLine((Vector2)(transform.position), (Vector2)(transform.position) + new Vector2(0.0f, obstacleInfo[1]), Color.yellow);
		}

		updateTimer += Time.deltaTime;

		if (updateTimer > updateFrequency) {

			netInput = new List<float>();

			// Nearest lake
			if (neuralNet.FeedLakePosition) {
				
				PriorityQueue<float, GameObject> lakesPQ = manager.getLakeMarkersSortedByDistance((Vector2)(transform.position));
				
				if (lakesPQ.Count > 0) {
					
					GameObject lake = lakesPQ.DequeueValue();
					
					Vector2 vecToLake = (Vector2)(lake.transform.position) - (Vector2)(transform.position);
					float lakeInputMag = Mathf.Exp(-vecToLake.magnitude / 10.0f); // TO DO: Fix these magic numbers throughout
					Vector2 lakeInputVec = vecToLake.normalized * lakeInputMag;
					netInput.Add(lakeInputVec.x);
					netInput.Add(lakeInputVec.y);
					//Debug.DrawLine((Vector2)(transform.position), (Vector2)(transform.position) + vecToLake, Color.cyan);
					
				} else {
					
					netInput.Add(0.0f);
					netInput.Add(0.0f);
				}
			}

			// Flies
			List<GameObject> closestFlies = manager.getFliesSortedByDistance((Vector2)(transform.Find("Mouth").position));
			GameObject fly = null;

			for (int i = 0; i < neuralNet.NumFlyPositions; i++) {

				if (closestFlies.Count > i) {

					fly = closestFlies[i];
					Vector2 vecToFly = (Vector2)(fly.transform.position) - (Vector2)(transform.Find("Mouth").position);
					float flyInputMag = Mathf.Exp(-vecToFly.magnitude / 10.0f);
					Vector2 flyInputVec = vecToFly.normalized * flyInputMag;
					netInput.Add(flyInputVec.x);
					netInput.Add(flyInputVec.y);

					//Debug.DrawLine((Vector2)(transform.FindChild("Mouth").position), (Vector2)(transform.FindChild("Mouth").position) + vecToFly, Color.cyan);

				// ith closest fly couldn't be found, so just return zero input
				} else {
					netInput.Add(0.0f);
					netInput.Add(0.0f);
				}
			}

			// Snakes
			PriorityQueue<float, GameObject> snakesPQ = manager.getSnakesSortedByDistance((Vector2)(transform.position));
			
			for (int i = 0; i < neuralNet.NumSnakePositions; i++) {
				
				if (snakesPQ.Count > i) {

					KeyValuePair<float, GameObject> snakeDistancePair = snakesPQ.Dequeue();

					GameObject snake = snakeDistancePair.Value;
					Vector2 vecToSnake = ((Vector2)(snake.transform.position) - (Vector2)(transform.position)).normalized * snakeDistancePair.Key;
					float snakeInputMag = Mathf.Exp(-vecToSnake.magnitude / 10.0f);
					Vector2 snakeInputVec = vecToSnake.normalized * snakeInputMag;
					netInput.Add(snakeInputVec.x);
					netInput.Add(snakeInputVec.y);

					// Fire a bubble if the closest snake is within range
					//Debug.Log("i = " + i + ", shotTimer = " + shotTimer + ", ShotReloadTime = " + ShotReloadTime + ", shot dist = " + neuralNet.getShotDistance() + ", actualDist = " + snakeDistancePair.Key);
					if (i == 0 && (shotTimer > ShotReloadTime) && (snakeDistancePair.Key < neuralNet.getShotDistance())) {
						if (mouthScript.SprayWater(true, (Vector2?)(snake.transform.position))) {
							shotTimer = 0.0f;
						}
					}
					
					// ith closest snake couldn't be found, so just return zero input
				} else {
					netInput.Add(0.0f);
					netInput.Add(0.0f);
				}
			}


			// Obstacles
			PriorityQueue<float, GameObject> obstaclesPQ = manager.getObstaclesSortedByDistance((Vector2)(transform.position));
			GameObject obstacle = null;
			
			for (int i = 0; i < neuralNet.NumObstaclePositions; i++) {
				
				if (obstaclesPQ.Count > i) {
					
					obstacle = obstaclesPQ.DequeueValue();
					Vector2 vecToObstacle = (Vector2)(obstacle.transform.position) - (Vector2)(transform.Find("Mouth").position);
					float obstacleInputMag = Mathf.Exp(-vecToObstacle.magnitude / 10.0f);
					Vector2 obstacleInputVec = vecToObstacle.normalized * obstacleInputMag;
					netInput.Add(obstacleInputVec.x);
					netInput.Add(obstacleInputVec.y);
					//Debug.DrawLine((Vector2)(transform.position), (Vector2)(transform.position) + vecToObstacle, Color.cyan);
					
					// ith closest obstacle couldn't be found, so just return zero input
				} else {
					netInput.Add(0.0f);
					netInput.Add(0.0f);
				}
			}

			// Another obstacle input method (TO DO: Maybe get rid of this or make it better?)
			if (neuralNet.FeedObstacleInfo) {

				obstacleInfo = GetObstacleInfo();

	            netInput.Add(obstacleInfo[0]);
	            netInput.Add(obstacleInfo[1]);
			}

			if (neuralNet.FeedOwnVelocity) {
				netInput.Add(GetComponent<Rigidbody2D>().velocity.x);
				netInput.Add(GetComponent<Rigidbody2D>().velocity.y);
			}

			if (neuralNet.FeedWaterLevel) {
				// Rescale to [0, 1] so it doesn't dominate other inputs
				netInput.Add(playerInfo.waterLevel / 100.0f);
			}

			netInputArr = netInput.ToArray();

			updateTimer = 0.0f;
		}
	}

	private float[] GetObstacleInfo() {

		float[] tempResult = {0.0f, 0.0f, 0.0f, 0.0f};
		float[] result = {0.0f, 0.0f};

		int layerMask = 0;
		bool obstacleFound;
		
		foreach (string layerName in obstacleLayerNames) {
			layerMask = layerMask | (1 << LayerMask.NameToLayer(layerName));
		}
		
		// Use four points around the frog to determine if it can move along a given direction
		/*
		Vector2[] circlePoints = new Vector2[4];
		circlePoints[0] = circleCollider.center + conservativeMultiplier * new Vector2(circleCollider.radius, circleCollider.radius);
		circlePoints[1] = circleCollider.center + conservativeMultiplier * new Vector2(-circleCollider.radius, -circleCollider.radius);
		circlePoints[2] = circleCollider.center + conservativeMultiplier * new Vector2(circleCollider.radius, -circleCollider.radius);
		circlePoints[3] = circleCollider.center + conservativeMultiplier * new Vector2(-circleCollider.radius, circleCollider.radius);
		*/

		Vector2[] circlePoints = new Vector2[1];
		circlePoints[0] = circleCollider.offset;

		for (int i = 0; i < 4; i++) {

			obstacleFound = false;

			for (float mag = obstacleDistancePrecision; mag <= obstacleDetectionRange; mag += obstacleDistancePrecision) {

				Vector2 rayCastVec = MathsHelper.rotateVector(new Vector2(1.0f, 0.0f), (float)i * 90.0f);
				
				foreach (Vector2 circlePoint in circlePoints) {
					
					RaycastHit2D[] rayHits = Physics2D.RaycastAll((Vector2)(transform.position) + circlePoint, rayCastVec, mag, layerMask);
					
					foreach (var rayHit in rayHits)
					{
						// As the ray includes the object itself skip it.
						if (rayHit.collider.gameObject.name == gameObject.name) {
							continue;
						}

						// If the obstacle is really close then return a number close to 1.
						// The further away the obstacle, the closer the input is to 0.
						tempResult[i] = (obstacleDetectionRange - mag) / obstacleDetectionRange;
						//Debug.DrawLine((Vector2)(transform.position), (Vector2)(transform.position) + mag * rayCastVec, Color.red);
						obstacleFound = true;
						break;
					}
					
					if (obstacleFound) {
						break;
					}
				}

				if (obstacleFound) {
					break;
				}
			}
		}

		// Instead of giving the neural net all four values, just give the closest horizontal
		// and vertical values. Left and Up are positive, Right and Down are negative.
		if (Mathf.Abs(tempResult[0]) > Mathf.Abs(tempResult[2])) {
			result[0] = tempResult[0];
		} else {
			result[0] = -tempResult[2];
		}

		if (Mathf.Abs(tempResult[1]) > Mathf.Abs(tempResult[3])) {
			result[1] = tempResult[1];
		} else {
			result[1] = -tempResult[3];
		}

		return result;
	}

	public override Vector2 GetSteering()
	{
		if (shotTimer < ShotSpinTime) {
			return Vector2.zero;
		}

		if ((netInputArr != null) && (netInputArr.Length != 0)) {
			float[] netOutput = neuralNet.CalculateOutput(netInputArr);
			Vector2 steering = new Vector2(netOutput[0], netOutput[1]);
			//if (steering.magnitude < minSteeringMag) {
				//steering = steering.normalized * minSteeringMag;
			//}
			//return steering * 4.0f;
			return steering * 30.0f;
		} else {
			return Vector2.zero;
		}
	}

	// Load the neural net from the file.
	private void LoadNetFromFile()
	{
		BinaryFormatter bf = new BinaryFormatter();
		
		FileStream netFile = File.Open(fileName, FileMode.Open);
		neuralNet = (NeuralNet)bf.Deserialize(netFile);
		neuralNet.ParentFrog = gameObject;
		netFile.Close();
	}
}