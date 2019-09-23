using UnityEngine;
using System.Collections;

public class ChangeDifficulty : MonoBehaviour {

	public int increment;
	public GameObject otherArrow;

	private TextMesh textMesh;
	private GameMaster gameMaster;
	
	void Awake() {
		textMesh = GameObject.Find("DifficultyDisplay").GetComponent<TextMesh>();
		gameMaster = GameObject.Find("GameMaster").GetComponent<GameMaster>();
	}
	
	// Use this for initialization
	void Start () {
		textMesh.text = "Difficulty: " + gameMaster.difficulties[gameMaster.difficulty];
	}
	
	void OnMouseDown()
	{
		gameMaster.difficulty = gameMaster.difficulty + increment;

		if ((gameMaster.difficulty <= 0) || (gameMaster.difficulty >= (gameMaster.difficulties.Length - 1))) {
			gameMaster.difficulty = Mathf.Clamp(gameMaster.difficulty, 0, gameMaster.difficulties.Length - 1);
			gameObject.SetActive(false);
		}

		textMesh.text = "Difficulty: " + gameMaster.difficulties[gameMaster.difficulty];

		otherArrow.SetActive(true);
	}
}
