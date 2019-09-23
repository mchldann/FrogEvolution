using UnityEngine;
using System.Collections;

public class SceneLoader : MonoBehaviour 
{
	public string sceneName;
	public int difficultyIncrement = 0;
	public bool ignoreGameMaster = false;

	private GameMaster gameMaster;

	// Use this for initialization
	void Start () {
		if(!ignoreGameMaster) {
			GameObject gameMasterGameObj = GameObject.Find("GameMaster");
			if (gameMasterGameObj != null) {
				gameMaster = gameMasterGameObj.GetComponent<GameMaster>();
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnMouseDown()
	{
		if (ignoreGameMaster) {
			Application.LoadLevel(sceneName);
			return;
		}
			
		// Increase the difficulty if required
		GameObject gameMasterGameObj = GameObject.Find("GameMaster");
		if (gameMasterGameObj != null) {
			GameMaster gameMaster = gameMasterGameObj.GetComponent<GameMaster>();
			gameMaster.difficulty += difficultyIncrement;
			gameMaster.difficulty = Mathf.Clamp(gameMaster.difficulty, 0, gameMaster.difficulties.Length - 1);
		}

		if (sceneName == "") {
			if (gameMaster.currentLevel != "") {
				Application.LoadLevel(gameMaster.currentLevel);
			} else {
				// Default if we don't know where to go
				Application.LoadLevel("Menu");
			}
		} else {
			gameMaster.currentLevel = sceneName;
			Application.LoadLevel(sceneName);
		}	
	}

	void OnMouseEnter()
	{
		transform.localScale *= 1.5f;
	}

	void OnMouseExit()
	{
		transform.localScale /= 1.5f;
	}
}
