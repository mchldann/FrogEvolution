using UnityEngine;
using System.Collections;

// This script is designed to stop the trees overlapping and causing weird lighting effects.
// Another option would be to use the sprite sorting layer.
public class StopZConflicts : MonoBehaviour {

	void Awake () {

		foreach (Transform child in transform)
		{
			child.transform.position = new Vector3(child.transform.position.x,
			                                       child.transform.position.y,
			                                       1.0f + 0.001f * child.transform.position.y + 0.00001f * child.transform.position.x);
		}
	}
}
