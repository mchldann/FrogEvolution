using UnityEngine;
using System.Collections;

public class CameraMovementRTS : MonoBehaviour {

	// I went a bit over the top with variables trying to make the feel of the camera nice!
	public float scrollSpeed;
	public float scrollPctOfScreen;
	public float minScrollPct;
	public float extraScrollFactorAtEdge;
	public float AdditionalLeftBuffer;
	public float AdditionalRightBuffer;
	public float AdditionalTopBuffer;
	public float AdditionalBottomBuffer;

	// To ensure that the screen size gives the player a decent view
	public float RequiredWidth = 10.0f;
	public float RequiredHeight = 10.0f;
	
	void Update () {

		Camera.main.orthographicSize = Mathf.Max(RequiredWidth / Camera.main.aspect, RequiredHeight);

		float nonScrollWidth = 0.5f * Screen.width * scrollPctOfScreen;
		float nonScrollHeight = 0.5f * Screen.height * scrollPctOfScreen;

		float nonScrollAmount = Mathf.Min(nonScrollWidth, nonScrollHeight);

		float scrollPct;
		
		float mouseXPos = Mathf.Clamp(Input.mousePosition.x, 0.0f, Screen.width);
		float mouseYPos = Mathf.Clamp(Input.mousePosition.y, 0.0f, Screen.height);

		if (mouseXPos == 0.0f || mouseXPos == Screen.width || mouseYPos == 0.0f || mouseYPos == Screen.height) {
			scrollPct = extraScrollFactorAtEdge;
		} else {
			scrollPct = 1.0f;
		}


		if (mouseXPos < nonScrollAmount) {
			scrollPct *= (minScrollPct + (1.0f - minScrollPct) * (nonScrollAmount - mouseXPos) / nonScrollAmount);
			transform.Translate(Vector3.right * -scrollSpeed * scrollPct * Time.deltaTime);
		}
		
		if (mouseXPos > (Screen.width - nonScrollAmount)) {
			scrollPct *= (minScrollPct + (1.0f - minScrollPct) * (mouseXPos - (Screen.width - nonScrollAmount)) / nonScrollAmount);
			transform.Translate(Vector3.right * scrollSpeed * scrollPct * Time.deltaTime);
		}
		
		if (mouseYPos < nonScrollAmount) {
			scrollPct *= (minScrollPct + (1.0f - minScrollPct) * (nonScrollAmount - mouseYPos) / nonScrollAmount);
			transform.Translate(Vector3.up * -scrollSpeed * scrollPct * Time.deltaTime);
		}
		
		if (mouseYPos > (Screen.height - nonScrollAmount)) {
			scrollPct *= (minScrollPct + (1.0f - minScrollPct) * (mouseYPos - (Screen.height - nonScrollAmount)) / nonScrollAmount);
			transform.Translate(Vector3.up * scrollSpeed * scrollPct * Time.deltaTime);
		}
		
		// Clamp the camera position to the part of the world we want to see
		float xBuffer, yBuffer;
		
		if (Camera.main.aspect > 1.0f) {
			xBuffer = Camera.main.orthographicSize * Camera.main.aspect;
			yBuffer = Camera.main.orthographicSize;
		} else {
			xBuffer = Camera.main.orthographicSize;
			yBuffer = Camera.main.orthographicSize * Camera.main.aspect;
		}
		
		float xPos = Mathf.Clamp (transform.position.x,
		                          GameObject.Find ("LeftBoundary").transform.position.x + xBuffer - AdditionalLeftBuffer,
		                          GameObject.Find ("RightBoundary").transform.position.x - xBuffer + AdditionalRightBuffer);
		
		float yPos = Mathf.Clamp (transform.position.y,
		                          GameObject.Find ("BottomBoundary").transform.position.y + yBuffer - AdditionalBottomBuffer,
		                          GameObject.Find ("TopBoundary").transform.position.y - yBuffer + AdditionalTopBuffer);
		
		transform.position = new Vector3(xPos, yPos, transform.position.z);
	}
}
