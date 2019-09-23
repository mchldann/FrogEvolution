using UnityEngine;
using System.Collections;

public class AppleTreeTargeter : Targeter {

	private Vector2 treePosition;

	public void Start() {
		GameObject[] trees = GameObject.FindGameObjectsWithTag("AppleTree");
		treePosition = trees[Random.Range(0, 4)].transform.position;
	}

	public void Update() {

		// Change trees if we're stuck
		ObstacleAvoider avoider = GetComponent<ObstacleAvoider>();
		if ((avoider != null) && (avoider.isStuck)) {
			UpdateTree();
		}
	}

	public override Vector2? GetTarget ()
	{
		return (Vector2?)treePosition;
	}

	public void UpdateTree()
	{
		GameObject[] trees = GameObject.FindGameObjectsWithTag("AppleTree");
		var newTreePos = treePosition;

		while (newTreePos == treePosition)
			treePosition = trees[Random.Range(0, 4)].transform.position;
	}
}
