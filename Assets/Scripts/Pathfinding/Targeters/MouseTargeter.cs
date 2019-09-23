using UnityEngine;
using System.Collections;

public class MouseTargeter : Targeter {
	
	private Vector2? _target = null;
	public bool flyRTS = false;
	public bool selected = false;

	// Update is called once per frame
	void Update () {

		if(flyRTS && selected) {
			FollowMouse();
		}
		else {
			if (!flyRTS) {
				//FollowMouse();
			}
		} 
		
	}

	public override Vector2? GetTarget ()
	{
		return _target;
	}

	public void StopTargeting() {
		_target = (Vector2)(transform.position);
	}

	private void FollowMouse()
	{
		// Use right-click to move
		if(Input.GetMouseButtonDown(1)) {
			Vector2 clickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			_target = clickPos;

			// Cease overriding rotation (used when firing a projectile) upon clicking
			GetComponent<Movement>().StopOverrideRotation();
		}
	}
}
