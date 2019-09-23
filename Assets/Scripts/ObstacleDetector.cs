using UnityEngine;
using System.Collections;

public class ObstacleDetector : MonoBehaviour {

	private Hashtable nearbyObstacles;
	
	void Awake () {
		nearbyObstacles = new Hashtable();
	}
	
	// Update is called once per frame
	void Update () {

		PredatorStateMachine psm = gameObject.GetComponentInParent<PredatorStateMachine>();

		if (psm != null) {
			if (nearbyObstacles.Count > 0) {
				psm.nearObstacle = true;
			} else {
				psm.nearObstacle = false;
			}
		}
	}

	public void OnTriggerEnter2D(Collider2D other) 
	{
		if (!nearbyObstacles.Contains(other)) {
			nearbyObstacles.Add(other, null);
		}
	}
	
	
	public void OnTriggerStay2D(Collider2D other) 
	{
		if (!nearbyObstacles.Contains(other)) {
			nearbyObstacles.Add(other, null);
		}
	}

	public void OnTriggerExit2D(Collider2D other) 
	{
		if (nearbyObstacles.Contains(other)) {
			nearbyObstacles.Remove(other);
		}
	}
}
