using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DragSelectFlies : MonoBehaviour 
{
	public Color vertexColor;
	private List<Transform> flies;
	private bool drawing;
	private Vector2 startPos;
	public GameObject mouseEffects;
	private GameObject mouseEffects_I;
	private ParticleSystem mouseEffectsParticles;

	// Use this for initialization
	void Start () 
	{
		Time.timeScale = 1f;
		drawing = false;
		flies = new List<Transform>();
		mouseEffects_I = (GameObject)Instantiate(mouseEffects);
		mouseEffectsParticles = mouseEffects_I.GetComponent<ParticleSystem>();
	}
	

	// Update is called once per frame
	void Update () 
	{
		if(Input.GetMouseButtonDown(0)) {
			if(!drawing) {
				drawing = true;
				startPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);			
			}
		}
		
		if(Input.GetMouseButtonDown(1)) {
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			Debug.Log(ray.origin);
			mouseEffectsParticles.transform.position = ray.GetPoint(Camera.main.transform.position.z * -1);
			mouseEffectsParticles.time = 0f;
			mouseEffectsParticles.Play();
		}

		if(drawing) {
			DrawBox(startPos, Camera.main.ScreenToWorldPoint(Input.mousePosition));
		}

		if (Input.GetMouseButtonUp(0)) {
			// Calculate the boundaries that select flies will fall into.
			Vector2 endPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			float minx = Mathf.Min(startPos.x, endPos.x);
			float maxx = Mathf.Max(startPos.x, endPos.x);
			float miny = Mathf.Min(startPos.y, endPos.y);
			float maxy = Mathf.Max(startPos.y, endPos.y);
			// Update select flies using the calcaluate min and max boundaries.
			UpdateSelectedFlies(new Vector2(minx, miny), new Vector2(maxx, maxy));
			// Were done selecting so cleanup.
			drawing = false;
			GetComponent<MeshFilter>().mesh.Clear();
		}
		
		// Check for player key presses for selecting a number of flies from selected group.
		UpdateNumFlies();
	}


	// Draw the graphical representation of the selector.
	private void DrawBox(Vector2 start, Vector2 end)
	{
		//Debug.DrawLine(start, end, Color.white,0.0f, true);
		Mesh mesh = new Mesh();

		Vector3[] verts = new Vector3[4];
		int[] triangles = new int[6];
		Color32[] color = new Color32[4];

		verts[0] = new Vector3(start.x, start.y, -1f);
		verts[1] = new Vector3(end.x, end.y, -1f);
		verts[2] = new Vector3(start.x, end.y, -1f);
		verts[3] = new Vector3(end.x, start.y, -1f);

		triangles[0] = 0;
		triangles[1] = 1;
		triangles[2] = 2;
		triangles[3] = 1; // if 0 and 
		triangles[4] = 0; // if 1 then it doesn't work, which confuses me.
		triangles[5] = 3;

		color[0] = vertexColor;
		color[1] = vertexColor;
		color[2] = vertexColor;
		color[3] = vertexColor;

		mesh.vertices = verts;
		mesh.uv = new Vector2[]{new Vector2(verts[0].x, verts[0].y), new Vector2(verts[1].x, verts[1].y), new Vector2(verts[2].x, verts[2].y), new Vector2(verts[3].x, verts[3].y)};
		mesh.normals = new Vector3[]{Vector3.back - verts[0], Vector3.back - verts[1], Vector3.back - verts[2], Vector3.back - verts[3]};
		mesh.colors32 = color;
		mesh.triangles = triangles;

		GetComponent<MeshFilter>().mesh = mesh;
	}


	// See if the fly objects of this parent are within
	// the box boundaries. If so, set their mouse property to selected.
	private void UpdateSelectedFlies(Vector2 boundsStart, Vector2 boundsEnd)
	{
		foreach (Transform fly in GetComponentInChildren<Transform>()) {
			if (fly.tag == "Fly") {
				// Check if fly's current position is within the boundaries.
				if (fly.transform.position.x >= boundsStart.x && fly.transform.position.x <= boundsEnd.x &&
					fly.transform.position.y >= boundsStart.y && fly.transform.position.y <= boundsEnd.y) {
					fly.GetComponent<MouseTargeter>().selected = true;
					fly.transform.Find("FlyGlow").gameObject.SetActive(true);
					
					// Only add the fly if it isn't currently contained in the list.
					if(!flies.Contains(fly))
						flies.Add(fly);
				}
				else {
					fly.GetComponent<MouseTargeter>().selected = false;
					fly.transform.Find("FlyGlow").gameObject.SetActive(false);
				}
			}
		}
	}


	// Check to see if the user wants a certain number of flies from the
	// selected group.
	private void UpdateNumFlies()
	{	
		// First remove any flies that may have been eaten.
		for(int i = 0; i < flies.Count; ++i) {
			if(flies[i] == null)
				flies.RemoveAt(i);
		}

		int numWanted = flies.Count;

		if (Input.GetKeyDown(KeyCode.Alpha1)) {
			numWanted = 1;
		} else if (Input.GetKeyDown(KeyCode.Alpha2)) {
			numWanted = 2;
		} else if (Input.GetKeyDown(KeyCode.Alpha3)) {
			numWanted = 3;
		} else if (Input.GetKeyDown(KeyCode.Alpha4)) {
			numWanted = 4;
		} else if (Input.GetKeyDown(KeyCode.Alpha5)) {
			numWanted = 5;
		}

		// If they want a higher or equal number than the flies selected
		// do nothing and just keep the current flies selected.
		if (numWanted >= flies.Count)
			return;

		int numToRemove = flies.Count - numWanted;

		//Debug.Log(flies.Count);
		//Debug.Log(numWanted);
		//Debug.Log(numToRemove);

		// Sort flies in order of furthest from mouse pointer
		// to closest. This is done so we remove the ones that
		// are further away before the closer ones.
		Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		Vector2 mousePos = new Vector2(mouseRay.origin.x, mouseRay.origin.y);
		flies.Sort( delegate(Transform fly1, Transform fly2) {
			if (Vector2.Distance(mousePos, fly1.position) < Vector2.Distance(mousePos, fly2.position)) 
				return 1;
			if (Vector2.Distance(mousePos, fly1.position) > Vector2.Distance(mousePos, fly2.position)) 
				return -1;
			else
				return 0;
		});

		for (int i = 0; i < numToRemove; ++i) {
			flies[i].GetComponent<MouseTargeter>().selected = false;
			flies[i].transform.Find("FlyGlow").gameObject.SetActive(false);
		}

		// Separate for loop to deal with removing elements from the collection.
		// Once an element is removed the other will be renumbered, so we use
		// constant r=0 to remove at the 0th index and i to keep track of removal progress.
		for (int i = 0, r = 0; i < numToRemove; ++i) {
			flies.RemoveAt(r);
		}
	}
}
