using UnityEngine;
using System.Collections;

public class DrawGUIFlyGame : MonoBehaviour 
{
	private int heartSize = 20;
	//private int heartSeparation = 10;

	public Sprite Fly;
	public Sprite Egg;
	public Sprite Snake;
	public Sprite ScoreS;
	public GUISkin skin;
	public GUIStyle pauseText;
	public GameObject player;

	private Texture2D eggTex, snakeTex, scoreTex, flyTex;
	private bool isPaused;
	private FlyPlayerInfo playerInfo;

	void Start () 
	{
		isPaused = false;
		flyTex = SpriteToTexture(Fly);
		eggTex = SpriteToTexture(Egg);
		snakeTex = SpriteToTexture(Snake);
		scoreTex = SpriteToTexture(ScoreS);
	}


	void Update ()
	{
		CheckForPause();
	}


	void OnGUI () 
	{
		GUI.skin = skin;

		GUI.Box (new Rect (10, 10, 100, 120), "");

		//int health = PlayerInfo.GetMainFrogHealth();

		// This could probably be made better by using GUI groups. 
		GUI.DrawTexture(new Rect(20, 20, heartSize, heartSize), flyTex, ScaleMode.ScaleToFit, true, 0.0f);
		GUI.DrawTexture(new Rect(20, 45, heartSize, heartSize), eggTex, ScaleMode.ScaleToFit, true, 0.0f);
		GUI.DrawTexture(new Rect(20, 70, heartSize, heartSize), snakeTex, ScaleMode.ScaleToFit, true, 0.0f);
		GUI.DrawTexture(new Rect(20, 95, heartSize, heartSize), scoreTex, ScaleMode.ScaleToFit, true, 0.0f);

		GUI.Label (new Rect (40, 20, 120, 20), ": " + FlyPlayerInfo.NumFlies);
		GUI.Label (new Rect (40, 45, 120, 20), ": " + FlyPlayerInfo.SelectedFliesResource1);
		GUI.Label (new Rect (40, 70, 120, 20), ": " + FlyPlayerInfo.SelectedFliesResource2);
		GUI.Label (new Rect (40, 95, 120, 20), ": " + FlyPlayerInfo.PlayerScore);

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
				FlyPlayerInfo.SetNewGame();
				Application.LoadLevel("MenuA2");
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


	// Helper function to convert sprites to textures.
	// Follows the code from http://answers.unity3d.com/questions/651984/convert-sprite-image-to-texture.html
	private Texture2D SpriteToTexture(Sprite sprite)
	{
		// Create a new empty texture with the dimensions of the sprite image.
		Texture2D texture = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
		// Get the pixels corresponding to this sprite from the sprite sheet.
		Color[] pixels = sprite.texture.GetPixels((int)sprite.textureRect.x, (int)sprite.textureRect.y, (int)sprite.textureRect.width, (int)sprite.textureRect.height);
		// Fill the new texture. 
		texture.SetPixels(pixels);
		// Must be called to set changes made via SetPixels.
		texture.Apply();

		return texture;
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
