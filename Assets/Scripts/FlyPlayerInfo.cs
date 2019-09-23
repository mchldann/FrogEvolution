using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FlyPlayerInfo : MonoBehaviour 
{
	private float resource1;
	private float resource2;
	private static bool newGame = true;
	private static float flySpawnTracker;
	private static float score;
	private static int numFlies;
	private static List<FlyPlayerInfo> flies_info;
	private static List<MouseTargeter> flies_mousetarget;
	private static List<GameObject> resourceObjects;
	public GameObject resource;
	public GameObject flyPlayer;
	public float interactionDistance;
	public float scoringInteractionDistance;
	public int resourceIncrement;
	public int scoringIncrementModifier;
	public int maxResource;
	public GameObject scoringLocation;
	public float flySpawnRate;

	public int Resource1 {get{return (int)resource1;} }
	public int Resource2 {get{return (int)resource2;} }
	public static int PlayerScore { get{return (int)score;} }
	public static int NumFlies { get{return (int)numFlies;}}

	public static void SetNewGame()
	{
		newGame = true;
	}

	public static int SelectedFliesResource1 
	{ 
		get {
			float r = 0f;

			for (int i = 0; i < flies_mousetarget.Count; ++i) {
				MouseTargeter m = flies_mousetarget[i];
				if(m == null)
					continue;
				if(m.selected) {
					r += flies_info[i].Resource1;
				}
			}

			return (int)r;
		}
	}

	public static int SelectedFliesResource2 
	{ 
		get {
			float r = 0f;
			
			for (int i = 0; i < flies_mousetarget.Count; ++i) {
				MouseTargeter m = flies_mousetarget[i];
				if(m == null)
					continue;
				if(m.selected && m.gameObject != null) {
					r += flies_info[i].Resource2;
				}
			}
			
			return (int)r;
		}
	}

	public static void DecrementFlyCount()
	{
		numFlies -= 1;
		if (numFlies == 0) {
			newGame = true;
			Application.LoadLevel("DeathSplashA2");
		}
	}

	void Awake()
	{
		// we need this bool as we dont want
		// new flys that are spawned to destroy the 
		// static member variables.
		if (newGame) {
			resourceObjects = null;
			flies_info = null;
			flies_mousetarget = null;
			score = 0;
			numFlies = 0;
			flySpawnTracker = 0;
			newGame = false;
		}
	}

	void Start()
	{
		// Singleton style for the resource trees.
		if (resourceObjects == null) {
			resourceObjects = new List<GameObject>();
			
			foreach (Transform r in resource.GetComponentsInChildren<Transform>()) {
				resourceObjects.Add(r.gameObject);
			}

			score = 0;
		}

		if(flies_info == null)
			flies_info = new List<FlyPlayerInfo> ();
		if(flies_mousetarget == null)
			flies_mousetarget = new List<MouseTargeter> ();

		numFlies += 1;
		flies_info.Add((FlyPlayerInfo)GetComponent<FlyPlayerInfo>());
		flies_mousetarget.Add((MouseTargeter)GetComponent<MouseTargeter>());

		resource1 = 0;
		resource2 = 0;
	}

	void Update()
	{
		UpdateResources();
		UpdateScore();
		SpawnNewFly();
	}

	// Just check if a fly is next to a resource and if so increment the appropriate one.
	private void UpdateResources()
	{
		foreach (GameObject resource in resourceObjects) {
				if (Vector2.Distance(resource.transform.position, transform.position) < interactionDistance) {
					if (resource.tag == "FlowerTree")
						resource1 = Mathf.Clamp(resource1 + Time.deltaTime * resourceIncrement, 0f, maxResource);
					else if (resource.tag == "AppleTree")
						resource2 = Mathf.Clamp(resource2 + Time.deltaTime * resourceIncrement, 0f, maxResource);
				}
		}
	}

	// Update the player score based on this flies gathered resources.
	private void UpdateScore()
	{
		// Start scoring from the resources earned.
		if (Vector2.Distance(scoringLocation.transform.position, transform.position) < scoringInteractionDistance) {
			float oldResource1 = resource1; 
			float oldResource2 = resource2;

			resource1 = Mathf.Max(0, resource1 - (Time.deltaTime * resourceIncrement * scoringIncrementModifier));
			resource2 = Mathf.Max(0, resource2 - (Time.deltaTime * resourceIncrement * scoringIncrementModifier));

			score += (oldResource1 - resource1) + (oldResource2 - resource2);
			// We also add this score to a seperate variable for the SpawnNewFly methods use.
			flySpawnTracker += (oldResource1 - resource1) + (oldResource2 - resource2); 
		}
	}

	// Check if a new fly is to be spawned.
	private void SpawnNewFly()
	{
		if(flySpawnTracker >= flySpawnRate) {
			GameObject newFly = (GameObject) Instantiate(flyPlayer, Vector3.zero, Quaternion.identity);
			newFly.transform.parent = transform.parent;
			// flySpawnRate is the score intervals at which a fly should be spawned. e.g. every 100, 200
			// or 300 points. This can conceptually be thought of as spending points earnt to spawn a fly.
			flySpawnTracker -= flySpawnRate;
		}
	}
}
