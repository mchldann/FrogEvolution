using UnityEngine;
using System.Collections;

public class SpawnFlies : MonoBehaviour {

	public GameObject flyPrefab;
	public int numFlies = 15;
	public int minFlies = 15;
	public float minDistanceFromPlayer = 5.0f;

	private float leftBoundary = -1.0f;
	private float rightBoundary = 1.0f;
	private float bottomBoundary = -1.0f;
	private float topBoundary = 1.0f;

	private float spawnBoundaryBuffer = 2.0f;

	// Use this for initialization
	void Start () {

		// Get map boundaries
		GameObject boundary;

		boundary = GameObject.Find("LeftBoundary");
		if (boundary != null) {
			leftBoundary = boundary.transform.position.x;
		}
		boundary = GameObject.Find("RightBoundary");
		if (boundary != null) {
			rightBoundary = boundary.transform.position.x;
		}
		boundary = GameObject.Find("BottomBoundary");
		if (boundary != null) {
			bottomBoundary = boundary.transform.position.y;
		}
		boundary = GameObject.Find("TopBoundary");
		if (boundary != null) {
			topBoundary = boundary.transform.position.y;
		}

		// Create flies
		for (int i = 0; i < numFlies; i++) {
			CreateFly(GetSpawnOffScreenPosition());
		}
	}

	void Update () {

		GameObject[] flies = GameObject.FindGameObjectsWithTag("Fly");
		if (flies.Length < minFlies) {
			CreateFly(GetSpawnOffScreenPosition());
		}
	}

	private void CreateFly(Vector3 position) {

		GameObject fly = Instantiate (flyPrefab, position, Quaternion.identity) as GameObject;
		fly.transform.parent = gameObject.transform;
		fly.tag = "Fly";
	}

	private Vector3 GetSpawnOffScreenPosition() {
		
		Vector3 spawnLocation;

		if (Random.value < 0.5f) {
			// Spawn on left or right boundary
			spawnLocation = new Vector3(Random.value < 0.5f? (leftBoundary - spawnBoundaryBuffer) : (rightBoundary + spawnBoundaryBuffer),
			                            Random.Range(bottomBoundary, topBoundary),
			                            0.0f);
		} else {
			// Spawn on top or bottom boundary
			spawnLocation = new Vector3(Random.Range(leftBoundary, rightBoundary),
			                            Random.value < 0.5f? (bottomBoundary - spawnBoundaryBuffer) : (topBoundary + spawnBoundaryBuffer),
			                            0.0f);
		}
		
		return spawnLocation;
	}

	// This method isn't actually used any more (there was a really rare error where flies would spawn stuck inside trees
	// even though the collider would normally resolve that...) It's just here in case we ever want it again.
	private Vector3 GetSpawnAnywherePosition() {
		
		Vector3 spawnLocation;
		GameObject player = GameObject.FindGameObjectWithTag("Player");
		
		if (player != null) {
			do {
				// Michael: For safety make the flies spawn away from the player
				// I got an error one time because a fly was destroyed before Flocking had initialised
				spawnLocation = new Vector3(Random.Range(leftBoundary, rightBoundary), Random.Range(bottomBoundary, topBoundary), flyPrefab.transform.position.z);
			} while (((Vector2)(spawnLocation - player.transform.position)).magnitude < minDistanceFromPlayer);
		} else {
			spawnLocation = new Vector3(Random.Range(leftBoundary, rightBoundary), Random.Range(bottomBoundary, topBoundary), flyPrefab.transform.position.z);
		}
		
		return spawnLocation;
	}
}
