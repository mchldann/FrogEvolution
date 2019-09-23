using UnityEngine;
using System.Collections;

public class ShowScore : MonoBehaviour {

	// Use this for initialization
	void Start () 
	{
		GetComponent<TextMesh>().text = "Score: " + FlyPlayerInfo.PlayerScore;
	}
	
}
