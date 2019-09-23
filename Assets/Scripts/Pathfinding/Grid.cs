using UnityEngine;
using System.Collections;

public class Grid {
	
	public static string OBSTACLES_LAYER_NAME = "Obstacles";
	public static string POND_LAYER_NAME = "Pond";
	public static string EGG_LAYER_NAME = "Eggs";

	private static float TEMP_BLOCKED_UPDATE_FREQ = 0.1f;
	private static float EGG_DETECTION_RADIUS = 1.0f;

	private float gridLeft;
	private float gridBottom;
	private float gridDivisionsPerUnit;
	private float divisionSize;
	private float blockDetectionRadius;
	private bool isEnemy;
	private Hashtable blockedSet;
	private Hashtable tempBlocked = new Hashtable();
	private float tempBlockedLastUpdated = 0.0f;
	Hashtable gridAreas = new Hashtable(); // <Node, setOfConnectedNodes>

	// These are in terms of divisions, not world distance
	private int gridWidth;
	private int gridHeight;
	
	private Node[][] squares;
	
	public Node[][] GetSquares() {
		return squares;
	}
	
	public float GetDivisionSize() {
		return divisionSize;
	}
	
	public Node GetClosestSquare(Vector2 pos) {
		
		int x = (int)Mathf.Round((pos.x - gridLeft) * gridDivisionsPerUnit);
		int y = (int)Mathf.Round((pos.y - gridBottom) * gridDivisionsPerUnit);

		x = Mathf.Clamp(x, 0, gridWidth - 1);
		y = Mathf.Clamp(y, 0, gridHeight - 1);

		return squares[x][y];
	}
	
	public bool IsBlocked(Node n) {
		if (n == null)
			return true;
		else {
			return blockedSet.Contains(n) || tempBlocked.Contains(n);
		}
	}

	public bool IsConnected(Node n1, Node n2) {

		if ((n1 == null) || (n2 == null)) {
			return false;
		}

		foreach (object o in gridAreas.Values) {

			if (((Hashtable)o).Contains(n1)) {
				if (((Hashtable)o).Contains(n2)) {
					return true;
				} else {
					return false;
				}
			} else if (((Hashtable)o).Contains(n2)) {
				return false;
			}
		}

		return false;
	}

	public Node GetClosestUnblockedNode(Node start) {

		if (start == null) {
			return null;
		}

		Queue openSet = new Queue();
		openSet.Enqueue(start);

		Node current;
		
		while (openSet.Count > 0) {
			
			current = (Node)(openSet.Dequeue());

			if (!blockedSet.Contains(current) && !tempBlocked.Contains(current)) {
				return current;
			}

			Node[] neighbours = current.GetNeighbours();
			
			foreach (Node n in neighbours) {
				if ((n != null) && (!openSet.Contains(n))) {
					openSet.Enqueue(n);
				}
			}
		}

		return null;
	}
	
	public void DebugDrawBlocked() {

		foreach (object o in blockedSet.Keys) {
			((Node)o).DebugDraw(divisionSize, Color.red);
		}

		foreach (object o in tempBlocked.Keys) {
			((Node)o).DebugDraw(divisionSize, Color.yellow);
		}
	}

	public void UpdateTempBlocked() {

		// We don't want to spam this every frame because the raycast is a bit expensive and it will be redundant if there are multiple creatures sharing the same grid.
		// The temporary blocked cells (from eggs) only apply to the snakes, since the player probably wants to target them
		if (isEnemy && ((Time.realtimeSinceStartup - tempBlockedLastUpdated) > TEMP_BLOCKED_UPDATE_FREQ)) {

			tempBlocked = new Hashtable();

			GameObject[] eggs = GameObject.FindGameObjectsWithTag("Egg");
			
			int layerMask = 1 << LayerMask.NameToLayer(EGG_LAYER_NAME);
			
			Vector2[] rayDirs = new Vector2[] {new Vector2(1.0f, 0.0f), new Vector2(-1.0f, 0.0f), new Vector2(0.0f, 1.0f), new Vector2(0.0f, -1.0f),
				new Vector2(1.0f, 1.0f), new Vector2(-1.0f, 1.0f), new Vector2(1.0f, -1.0f), new Vector2(-1.0f, -1.0f)};
			
			foreach (GameObject egg in eggs) {
				// 1.01 to stop rounding issue from skipping nodes
				for (float x = egg.transform.position.x - EGG_DETECTION_RADIUS; x <= egg.transform.position.x + EGG_DETECTION_RADIUS; x += divisionSize / 1.01f) {
					for (float y = egg.transform.position.y - EGG_DETECTION_RADIUS; y <= egg.transform.position.y + EGG_DETECTION_RADIUS; y += divisionSize / 1.01f) {
						
						Node n = GetClosestSquare(new Vector2(x, y));
						
						foreach (Vector2 ray in rayDirs) {
							if (!(tempBlocked.Contains(n)) && Physics2D.Raycast(n.GetPosition(), ray, blockDetectionRadius, layerMask)) { // A bit hacky...
								tempBlocked.Add(n, n);
								break;
							}
						}
					}
				}
			}

			tempBlockedLastUpdated = Time.realtimeSinceStartup;
		}
	}
	
	public Grid(float gridLeft, float gridRight, float gridBottom, float gridTop, float gridDivisionsPerUnit, float blockDetectionRadius, bool isEnemy) {
		
		this.gridLeft = gridLeft;
		this.gridBottom = gridBottom;
		this.gridDivisionsPerUnit = gridDivisionsPerUnit;
		this.divisionSize = 1.0f / gridDivisionsPerUnit;
		this.blockDetectionRadius = blockDetectionRadius;
		this.isEnemy = isEnemy;
		
		this.gridWidth = (int)((gridRight - gridLeft) * gridDivisionsPerUnit);
		this.gridHeight = (int)((gridTop - gridBottom) * gridDivisionsPerUnit);

		bool logTiming = false;

		squares = new Node[gridWidth][];
		
		blockedSet = new Hashtable();
		
		int layerMask = 1 << LayerMask.NameToLayer(OBSTACLES_LAYER_NAME);

		if (isEnemy) {
			int lakeMask = 1 << LayerMask.NameToLayer (POND_LAYER_NAME);
			layerMask = layerMask | lakeMask;
		}

		float timeNow = Time.realtimeSinceStartup;

		for (int i = 0; i < gridWidth; i++) {
			squares[i] = new Node[gridHeight];
		}
		
		for (int i = 0; i < gridWidth; i++) {
			for (int j = 0; j < gridHeight; j++) {
				Vector2 pos = new Vector2(gridLeft + (float)i / gridDivisionsPerUnit, gridBottom + (float)j / gridDivisionsPerUnit);
				squares[i][j] = new Node(i, j, pos);
			}
		}

		if (logTiming) {
			Debug.Log ("Took " + (Time.realtimeSinceStartup - timeNow) + " to allocate grid");
			timeNow = Time.realtimeSinceStartup;
		}

		for (int i = 0; i < gridWidth; i++) {
			
			for (int j = 0; j < gridHeight; j++) {
				
				Node[] neighbours = new Node[(int)NodeDirections.Last + 1];
				
				if (i > 0)
					neighbours[(int)NodeDirections.Left] = squares[i - 1][j];
				
				if (i < (gridWidth - 1))
					neighbours[(int)NodeDirections.Right] = squares[i + 1][j];
				
				if (j > 0)
					neighbours[(int)NodeDirections.Bottom] = squares[i][j - 1];
				
				if (j < (gridHeight - 1))
					neighbours[(int)NodeDirections.Top] = squares[i][j + 1];
				
				if ((i > 0) && (j > 0))
					neighbours[(int)NodeDirections.BottomLeft] = squares[i - 1][j - 1];
				
				if ((i > 0) && (j < (gridHeight - 1)))
					neighbours[(int)NodeDirections.TopLeft] = squares[i - 1][j + 1];
				
				if ((i < (gridWidth - 1)) && (j > 0))
					neighbours[(int)NodeDirections.BottomRight] = squares[i + 1][j - 1];
				
				if ((i < (gridWidth - 1)) && (j < (gridHeight - 1)))
					neighbours[(int)NodeDirections.TopRight] = squares[i + 1][j + 1];
				
				squares[i][j].SetNeighbours(neighbours);
				
				Vector2[] rayDirs = new Vector2[] {new Vector2(1.0f, 0.0f), new Vector2(-1.0f, 0.0f), new Vector2(0.0f, 1.0f), new Vector2(0.0f, -1.0f),
					new Vector2(1.0f, 1.0f), new Vector2(-1.0f, 1.0f), new Vector2(1.0f, -1.0f), new Vector2(-1.0f, -1.0f)};
				
				foreach (Vector2 ray in rayDirs) {
					if (Physics2D.Raycast(squares[i][j].GetPosition(), ray, blockDetectionRadius, layerMask)) { // A bit hacky...
						blockedSet.Add(squares[i][j], squares[i][j]);
						break;
					}
				}
			}
		}

		if (logTiming) {
			Debug.Log ("Took " + (Time.realtimeSinceStartup - timeNow) + " to set neighbours and raycast");
			timeNow = Time.realtimeSinceStartup;
		}

		/*
		// TO DO: Update this logic so it just fills unreachable areas
		// Fill holes
		ArrayList cornerBlocked = new ArrayList();
		for (int i = 0; i < gridWidth; i++) {
			for (int j = 0; j < gridHeight; j++) {
				
				Node leftNode = squares[i][j].GetNeighbours()[(int)NodeDirections.Left];
				Node rightNode = squares[i][j].GetNeighbours()[(int)NodeDirections.Left];
				Node bottomNode = squares[i][j].GetNeighbours()[(int)NodeDirections.Left];
				Node topNode = squares[i][j].GetNeighbours()[(int)NodeDirections.Left];
				
				if (!blockedSet.Contains(squares[i][j])
				    && (leftNode != null) && blockedSet.Contains(leftNode)
				    && (rightNode != null) && blockedSet.Contains(rightNode)
				    && (bottomNode != null) && blockedSet.Contains(bottomNode)
				    && (topNode != null) && blockedSet.Contains(topNode)) {
					
					cornerBlocked.Add(squares[i][j]);
				}
			}
		}
		foreach (object o in cornerBlocked) {
			Node n = (Node)o;
			blockedSet.Add(n, n);
		}

		if (logTiming) {
			Debug.Log ("Took " + (Time.realtimeSinceStartup - timeNow) + " to fill holes");
			timeNow = Time.realtimeSinceStartup;
		}
		*/

		Hashtable done = new Hashtable ();

		for (int i = 0; i < gridWidth; i++) {

			for (int j = 0; j < gridHeight; j++) {

				if (done.Contains(squares[i][j]) || blockedSet.Contains(squares[i][j])) {
					continue;
				}

				Hashtable connectedNodes = new Hashtable();
				Queue openQueue = new Queue();
				Hashtable openSet = new Hashtable();
				Node current;

				openQueue.Enqueue(squares[i][j]);
				openSet.Add(squares[i][j], squares[i][j]);

				while (openQueue.Count > 0) {

					current = (Node)(openQueue.Dequeue());

					connectedNodes.Add(current, current);

					// TO DO: Might have to fix this so that it doesn't include diagonal neighbours
					Node[] neighbours = current.GetNeighbours();

					foreach (Node n in neighbours) {
						if ((n != null) && (!openSet.Contains(n)) && (!blockedSet.Contains(n)) && (!connectedNodes.Contains(n))) {
							openQueue.Enqueue(n);
							openSet.Add(n, n);
						}
					}
				}
				foreach (object o in connectedNodes.Keys) {
					gridAreas.Add((Node)o, connectedNodes);
					done.Add((Node)o, (Node)o);
				}
			}
		}

		if (logTiming) {
			Debug.Log ("Took " + (Time.realtimeSinceStartup - timeNow) + " to find connected");
			timeNow = Time.realtimeSinceStartup;
		}
	}
}