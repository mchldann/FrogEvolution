using UnityEngine;
using System.Collections;

public class GameMaster : MonoBehaviour
{
	public static GameMaster gameMaster;
	
	public string[] difficulties = {"Easy", "Normal", "Hard", "Insane"};
	public int difficulty = 1;
	public string currentLevel;
	public bool SmartDemoSnakes = true;
	public bool FlyDemoObstacleAvoidance = true;

	void Awake()
	{
		if(gameMaster != null)
			Destroy(this.gameObject);
		else
			gameMaster = this;
		
		DontDestroyOnLoad(this);
	}
}