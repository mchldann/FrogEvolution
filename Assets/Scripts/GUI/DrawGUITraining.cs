using UnityEngine;
using System.Collections;

public class DrawGUITraining : MonoBehaviour 
{
	public GUISkin skin;
	public GUIStyle pauseText;
	public GAFrogController geneticAlgo;

	private bool isPaused;
	
	void Start () 
	{
		isPaused = false;
	}
	
	
	void Update ()
	{
		CheckForPause();
	}
	
	
	void OnGUI () 
	{
		GUI.skin = skin;
		
		GUI.Box (new Rect (10, 10, 80, 70), "");

		GUI.Label (new Rect (20, 20, 120, 20), "Epoch: " + geneticAlgo.CurrentEpoch);
		GUI.Label (new Rect (20, 45, 120, 20), "Batch: " + geneticAlgo.CurrentBatch);
		
		// Draw the pause menu
		if (isPaused) {
			int menuWidth = 300;
			int menuHeight = 220;
			
			// Center the menu on the screen.
			GUI.BeginGroup(new Rect (Screen.width / 2 - menuWidth / 2, Screen.height / 2 - menuHeight / 2, menuWidth, menuHeight));
			GUI.Box (new Rect (0, 0, menuWidth, menuHeight), "");
			GUI.Label(new Rect (79, 30, 100, 30), "Game Paused", pauseText);
			// Draw the button which will take the player back to the main menu.
			// And handle the situation in which it is pressed.
			if(GUI.Button(new Rect (100, 70, 100, 30), "Main Menu")) {
				UnPause();
				AStarTargeter.ClearGrids();
				Application.LoadLevel("Menu");
			}
			if(GUI.Button(new Rect (100, 110, 100, 30), "Resume")) {
				UnPause();
			}
			if(GUI.Button(new Rect (100, 150, 100, 30), "Exit Game")) {
				AppHelper.Quit();
			}
			GUI.EndGroup();
		}
	}
	
	private void UnPause() {
		Time.timeScale = 1;
		isPaused = false;
		PlayerInfo.isPaused = false;
	}
	
	
	private void CheckForPause() 
	{
		if (Input.GetKeyDown(KeyCode.Escape)) {
			if (isPaused) {
				UnPause();
			}
			else {
				Time.timeScale = 0;
				isPaused = true;
				PlayerInfo.isPaused = true;
			}
		}
	}
}