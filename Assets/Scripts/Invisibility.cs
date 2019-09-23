using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Invisibility : MonoBehaviour 
{
	public float visibleDistance;
	public GameObject flyPlayer;
	public GameObject smokePrefab;
	public GameObject shieldPrefab;
	private List<Transform> flies;
	private SpriteRenderer frogRenderer;
	private SpriteRenderer tongueRenderer;
	private GameObject smokeInst, shieldInst;
	private bool disappear;

	// Use this for initialization
	void Start () 
	{
		flies = new List<Transform>();
		foreach (Transform t in flyPlayer.GetComponentInChildren<Transform>()) {
			flies.Add(t);
		}

		frogRenderer = GetComponent<SpriteRenderer>();

		foreach (Transform t in GetComponentInChildren<Transform>()) {
			if (t.name == "Tongue") {
				tongueRenderer = t.gameObject.GetComponent<SpriteRenderer>();
			}
		}

		disappear = true;
	}
	
	// Update is called once per frame
	void Update () 
	{
		foreach (Transform fly in flies) {
			if (Vector2.Distance(fly.position, transform.position) <= visibleDistance) {
				frogRenderer.enabled = true;
				tongueRenderer.enabled = true;

				if(smokeInst == null)
					smokeInst = (GameObject)Instantiate(smokePrefab, new Vector3(transform.position.x, transform.position.y, transform.position.z - 1), Quaternion.identity);

				disappear = true;

				return;
			}
		}

		// If we got to this point the frog is disappearing so we will get rid of the
		// smoke particle system.
		if(smokeInst)
			Destroy(smokeInst);

		frogRenderer.enabled = false;
		tongueRenderer.enabled = false;

		// Show the invisible shield coming back on.
		if(shieldInst == null && disappear) {
			disappear = false;
			shieldInst = (GameObject)Instantiate(shieldPrefab, new Vector3(transform.position.x, transform.position.y, transform.position.z - 1), Quaternion.identity);
		} else if(shieldInst) {
			// Check to see if we need to cleanup the particle system object.
			ParticleSystem ps = shieldInst.GetComponent<ParticleSystem>();
			if (ps.time >= ps.duration) 
				Destroy(shieldInst);
		}
	}
}
