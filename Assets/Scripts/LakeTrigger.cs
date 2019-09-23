using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LakeTrigger : MonoBehaviour {
	
	public void OnTriggerEnter2D(Collider2D other) {
		if (other.gameObject.tag == "Player") {
			other.GetComponent<PlayerInfo>().SetUnderwater(true);
		} else if (other.gameObject.tag == "Predator") {
			other.gameObject.GetComponent<PredatorStateMachine>().Sink();
		}
	}

	public void OnTriggerStay2D(Collider2D other) {
		if (other.gameObject.tag == "Player") {
			other.GetComponent<PlayerInfo>().SetUnderwater(true);
		} else if (other.gameObject.tag == "Predator") {
			other.gameObject.GetComponent<PredatorStateMachine>().Sink();
		}
	}
	
	public void OnTriggerExit2D(Collider2D other) {
		if (other.gameObject.tag == "Player") {
			other.GetComponent<PlayerInfo>().SetUnderwater(false);
		} else if (other.gameObject.tag == "Predator") {
			other.gameObject.GetComponent<PredatorStateMachine>().Unsink();
		}
	}
}
