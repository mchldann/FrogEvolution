using UnityEngine;
using System.Collections;

public class RandomCompliment : MonoBehaviour 
{
	public string[] compliments;
	
	// Use this for initialization
	void Start () 
	{
		GameObject musicPlayer = GameObject.Find("Music");
		if (musicPlayer != null) {
			musicPlayer.transform.position = Vector3.zero;
		}

		GameObject gameMasterGameObj = GameObject.Find("GameMaster");
		
		// Give a different continue message if we're already on the hardest difficulty setting
		if (gameMasterGameObj != null) {
			GameMaster gameMaster = gameMasterGameObj.GetComponent<GameMaster>();
			if (gameMaster.difficulty >= (gameMaster.difficulties.Length - 1)) {
				GameObject continueButton = GameObject.Find("Continue");
				if (continueButton != null) {
					continueButton.GetComponent<TextMesh>().text = "Try that again...";
				}
			}
		}
		GetComponent<TextMesh>().text = compliments[Random.Range(0, compliments.Length)];
	}
}
