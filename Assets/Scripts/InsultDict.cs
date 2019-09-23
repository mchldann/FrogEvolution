using UnityEngine;
using System.Collections;

public class InsultDict : MonoBehaviour 
{
	public string[] insults;

	// Use this for initialization
	void Start () 
	{
		GameObject musicPlayer = GameObject.Find("Music");
		if (musicPlayer != null) {
			musicPlayer.transform.position = Vector3.zero;
		}

		GetComponent<TextMesh>().text = insults[Random.Range(0, insults.Length)];
	}
}
