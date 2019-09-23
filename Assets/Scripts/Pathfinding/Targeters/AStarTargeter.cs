using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

enum NodeDirections
{
	First = 0,
	Left = 0,
	Right = 1,
	Bottom = 2,
	Top = 3,
	BottomLeft = 4,
	TopLeft = 5,
	BottomRight = 6,
	TopRight = 7,
	Last = 7
};

public enum Mode
{
	AStarVanilla,
	AStarWithJPS,
	Direct
}

[RequireComponent(typeof(Collider2D))]
public class AStarTargeter : Targeter {
	
	private ArrayList path = null;
	private Vector2 targetPos;
	private Node goalNode;
	private float timeSinceUpdate = 0.0f;
	private float blockDetectionRadius;
	private ArrayList nodesExploredLastMove = new ArrayList(); // For drawing debug info

	private float minRecalculationTime = 0.2f;
	private float timeSinceRecalculated = 0.0f;

	private static int gridDivisionsPerSquare = 3; // Still runs ok on 2 if this is making things laggy
	private static Hashtable grids = new Hashtable();
	
	public float updateFrequency;
	public float moveNextNodeRadius = 0.2f;
	public bool drawDebug;
	public GameObject goalFlag;
	public Mode searchMode = Mode.AStarWithJPS;
	public Targeter underlyingTargeter;
	public bool isEnemy;
	
	public override Vector2? GetTarget ()
	{
		if (searchMode == Mode.Direct) {
			//return null; // Don't let the snakes move for now
			return underlyingTargeter.GetTarget();
		}

		if (path == null || path.Count == 0) {
			return null;
		} else {
			return (Vector2?)targetPos;
		}
	}

	public void StopTargeting() {
		targetPos = (Vector2)(transform.position);
		path = null;
	}
	
	public float DistanceFromGoal(Vector2 pos) {

		// Manhattan distance
		// This metric isn't really "admissable" now that we're allowing diagonal grid movement.
		//return (float)(Math.Abs(goalNode.GetPosition().x - pos.x) + Math.Abs(goalNode.GetPosition().y - pos.y));

		// Euclidean distance
		//return (goalNode.GetPosition() - pos).magnitude;

		// Customised metric for the type of grid we're using, which is soft of an analog of the Manhattan distance for grids with diagonal movement allowed.
		// Returns the diagonal distance required plus the leftover -- we can never reach the goal faster than this.
		float xDiff = Mathf.Abs((goalNode.GetPosition() - pos).x);
		float yDiff = Mathf.Abs((goalNode.GetPosition() - pos).y);

		if (xDiff > yDiff) {
			return Mathf.Sqrt(2.0f * yDiff * yDiff) + xDiff - yDiff;
		} else {
			return Mathf.Sqrt(2.0f * xDiff * xDiff) + yDiff - xDiff;
		}
	}
	
	public static void ClearGrids() {
		grids.Clear();
	}

	void Start () {

		Collider2D collider = GetComponent<Collider2D>();
		
		// This assumes that all scaling is equal (i.e. x scaling = y scaling) and the GameObject's collider
		// is a perfect square or circle (which it is for the snakes and frog).
		// "blockDetectionRadius" is the extra blocked zone that the grid creates around blocked nodes.
		if (collider.GetType() == typeof(CircleCollider2D)) {
			blockDetectionRadius = ((CircleCollider2D)collider).radius * transform.localScale.x;
		} else if (collider.GetType() == typeof(BoxCollider2D)) {
			blockDetectionRadius = ((BoxCollider2D)collider).size.x * transform.localScale.x;
		} else {
			Debug.Log("ERROR: Unsupported collider type!");
			blockDetectionRadius = 0.0f;
		}
		
		Grid grid;
		
		if (!grids.Contains(gameObject.tag)) {

			// Initialise the grid of nodes used for A*	
			grid = new Grid(GameObject.Find("LeftBoundary").transform.position.x,
			                GameObject.Find("RightBoundary").transform.position.x,
			                GameObject.Find("BottomBoundary").transform.position.y,
			                GameObject.Find("TopBoundary").transform.position.y,
			                (float)gridDivisionsPerSquare, blockDetectionRadius, isEnemy);
			
			grids.Add(gameObject.tag, grid);
			
		} else {

			// Use an existing grid (this will happen for the snakes after the first snake grid is created)
			grid = (Grid)(grids[gameObject.tag]);
		}
		
		// Set the goal at the player's position so that they won't start moving when the game starts
		goalNode = grid.GetClosestSquare(transform.position);
		if (goalNode == null) {
			Debug.Log("ERROR: Player placed in a bad position!");
		}
		
		// Hide the target flag
		if (goalFlag != null)
			goalFlag.GetComponent<SpriteRenderer>().enabled = false;
	}

	// Since the seek behaviour doesn't guarantee that the GameObject will move *exactly* to the next waypoint, it's
	// possible for creatures (most likely the snakes) to get stuck by drifting slightly off course. If they're stuck
	// ramming against an obstacle then hopefully recalculating the path will get them unstuck.
	public void ForceRecalculate() {

		if (timeSinceRecalculated > minRecalculationTime) {
			goalNode = null;
			timeSinceRecalculated = 0.0f;
		}
	}
	
	// Draw the current path (shows in the "scene" window, but you can turn on "Gizmos" to see it in on the game screen)
	void DebugDrawPath() {
		
		Grid grid = (Grid)(grids[gameObject.tag]);
		
		if ((path != null) && (path.Count > 0)) {
			Debug.DrawLine((Vector2)transform.position, (Vector2)path[0], Color.green);
			for (int i = 0; i < path.Count - 1; i++) {
				Debug.DrawLine((Vector2)path[i], (Vector2)path[i + 1], Color.green);
			}
		}
		
		foreach (object o in nodesExploredLastMove) {
			((Node)o).DebugDraw(grid.GetDivisionSize(), Color.blue);
		}
	}
	
	void Update() {

		// For frog training it was easier to just make the A* targeter directly target the frog
		// rather than changing all the snake components. With direct targeting, it just returns
		// the position of the frog so there's no need to update the A* path.
		if (searchMode == Mode.Direct) {
			return;
		}

		timeSinceRecalculated += Time.deltaTime;

		Grid grid = (Grid)(grids[gameObject.tag]);

		// Stop null reference errors when we reset all grids and return to the main menu
		if (grid == null) {
			return;
		}

		// Update the set of "temporary blocked" nodes (this is how the snakes avoid their own eggs)
		grid.UpdateTempBlocked();

		// Visual debugging
		if (drawDebug) {
			DebugDrawPath();
		}
		if (drawDebug) {
			grid.DebugDrawBlocked();
		}
		
		timeSinceUpdate += Time.deltaTime;

		// If updateFrequency > 0 then the path won't attempt to update on every frame.
		// This is pretty critical for the snakes - they'll cause a lot of slowdown otherwise!
		// We're still able to update fast enough the snake behaviour to look intelligent (0.2 sec is their current setting)
		if (timeSinceUpdate > updateFrequency) {
			
			timeSinceUpdate = 0.0f;

			// The underlying targeter tells us where we're trying to head.
			// For example, it could be a "mouse targeter" (i.e. move the frog to where we clicked)
			// or a "GameObject targeter" (i.e. when the snakes are targetting the frog).
			Vector2? tempTarget = underlyingTargeter.GetTarget();
			
			if (tempTarget != null) {
				
				// If the closest node to the target is blocked then return the nearest unblocked square
				Node tempGoal = grid.GetClosestSquare((Vector2)tempTarget);
				tempGoal = grid.GetClosestUnblockedNode(tempGoal);

				// Only update the path if target has changed
				if ((tempGoal != null) && (!tempGoal.Equals(goalNode))) {
					goalNode = tempGoal;
					
					ArrayList tempPath = GetAStarPath ();
					
					// Only update the current path if A* successfully found a path
					if (tempPath != null) {
						path = tempPath;
						
						if (goalFlag != null)
							goalFlag.transform.position = goalNode.GetPosition ();
						
						// Target the first waypoint in the path
						targetPos = (Vector2)path [0];
						
						// Show the target flag
						if (goalFlag != null)
							goalFlag.GetComponent<SpriteRenderer> ().enabled = true;
					}
				}
			}
		}
		
		if (path != null && path.Count > 0) {

			// Move to the next waypoint on the path when we're close-ish to the the current waypoint
			if ((targetPos - (Vector2)transform.position).magnitude < moveNextNodeRadius) {

				path.RemoveAt(0);
				
				if (path.Count > 0) {
					// There's another waypoint left, so go to it
					targetPos = (Vector2)path[0];
				} else {
					// Otherwise, we've arrived at the destination
					path = null;
				}
			}
		} else {
			// Hide the goal flag when there's no current path
			if (goalFlag != null)
				goalFlag.GetComponent<SpriteRenderer>().enabled = false;
		}
	}
	
	private ArrayList GetAStarPath() {

		// For timing how long the path took to calculate
		float startTime = Time.realtimeSinceStartup;
		
		Grid grid = (Grid)(grids[gameObject.tag]);
		
		Node startPoint = grid.GetClosestSquare((Vector2)transform.position);
		startPoint = grid.GetClosestUnblockedNode(startPoint);
		
		if (startPoint == null) {
			Debug.Log("ERROR: A* can't calculate a path because the start point is bad!");
			return null;
		}
		
		// Store the frontier nodes in a priority queue (implemented as a binary heap) for efficiency
		PriorityQueue<float, Node> frontier = new PriorityQueue<float, Node>();

		// Again for efficient reasons, use hashtables to store the open and closed sets
		Hashtable openSet = new Hashtable();
		Hashtable closedSet = new Hashtable();
		
		if (grid.IsBlocked(goalNode)) {
			return null;
		}

		frontier.Add(new KeyValuePair<float, Node>(DistanceFromGoal((Vector2)transform.position), startPoint));
		openSet.Add(startPoint, startPoint);

		// Don't attempt a path if the start and goal nodes are disconnected because we'll end up
		// searching the whole area the start node is connected to and taking a long time to run!
		if (!grid.IsConnected(startPoint, goalNode)) {
			return null;
		}
		
		Hashtable nodeParent = new Hashtable(); // <Node, parent>
		nodeParent.Add(startPoint, null);

		// G scores are stored in a map rather than in the nodes themselves,
		// because this way the grid can be shared between multiple creatures.
		Hashtable gScoresMap = new Hashtable(); // <Node, gScore>
		gScoresMap.Add (startPoint, 0.0f);

		Node currentNode = startPoint;
		
		nodesExploredLastMove.Clear();

		while (frontier.Count != 0) {

			currentNode = frontier.DequeueValue();

			// Remove current from openset
			openSet.Remove(currentNode);
			
			// Add current to closedset
			if (!closedSet.Contains(currentNode)) {
				closedSet.Add(currentNode, currentNode);
			}
			
			float currentNodeGScore = (float)(gScoresMap[currentNode]);
			
			// For drawing debug info
			nodesExploredLastMove.Add(currentNode);
			
			// If we've reached the goal then reconstruct the path
			if (currentNode.Equals(goalNode)) {
				
				ArrayList path = new ArrayList();
				
				while (currentNode != null) {
					path.Add(currentNode.GetPosition());
					currentNode = (Node)(nodeParent[currentNode]);
				}
				
				path.Reverse();
				
				int layerMask = 1 << LayerMask.NameToLayer(Grid.OBSTACLES_LAYER_NAME);

				// Snakes should avoid eggs and the pond
				if (isEnemy) {
					int lakeMask = 1 << LayerMask.NameToLayer (Grid.POND_LAYER_NAME);
					int eggMask = 1 << LayerMask.NameToLayer (Grid.EGG_LAYER_NAME);
					layerMask = layerMask | lakeMask | eggMask;
				}
				
				// Remove intermediate waypoints if there is a clear path to a later waypoint
				for (int i = 0; i < path.Count - 2; i++) {
					Vector2 rayDir = (Vector2)path[i + 2] - (Vector2)path[i];
					if (!Physics2D.Raycast((Vector2)path[i], rayDir, rayDir.magnitude, layerMask)
					    && !Physics2D.Raycast((Vector2)path[i] + new Vector2(0.0f, blockDetectionRadius), rayDir, rayDir.magnitude, layerMask)
					    && !Physics2D.Raycast((Vector2)path[i] + new Vector2(0.0f, -blockDetectionRadius), rayDir, rayDir.magnitude, layerMask)
					    && !Physics2D.Raycast((Vector2)path[i] + new Vector2(blockDetectionRadius, 0.0f), rayDir, rayDir.magnitude, layerMask)
					    && !Physics2D.Raycast((Vector2)path[i] + new Vector2(-blockDetectionRadius, 0.0f), rayDir, rayDir.magnitude, layerMask)) {
						path.RemoveAt(i + 1);
						i--;
					}
				}
				
				// We don't want the current position in the path
				if (path.Count > 0)
					path.RemoveAt(0); 
				
				if (path.Count == 0) {
					return null;
				} else {
					
					if (drawDebug) {
						Debug.Log(searchMode.ToString() + " calculated path in " + 1000.0f * (Time.realtimeSinceStartup - startTime) + " milliseconds");
					}
					
					return path;
				}
			}

			if (searchMode == Mode.AStarWithJPS) {
				
				ArrayList jpsSuccessors = identifySuccessors(grid, currentNode, (Node)(nodeParent[currentNode]), goalNode);
				
				foreach (object o in jpsSuccessors) {
					
					Node n = (Node)o;

					float tentG = currentNodeGScore + (n.GetPosition() - currentNode.GetPosition()).magnitude;
					float fsc = tentG + DistanceFromGoal(n.GetPosition());
					
					if (!grid.IsBlocked(n) && !closedSet.Contains(n)) {

						if (!openSet.Contains(n)) {

							nodeParent.Add(n, currentNode);
							gScoresMap.Add(n, tentG);
							frontier.Add(new KeyValuePair<float, Node>(fsc, n));
							openSet.Add(n, n);

						} else {
							
							// Neighbour is already in the open set. If the tentative g score is lower
							// than the existing one, update the parent and existing f and g scores.
							float existingGScore = (float)(gScoresMap[n]);
							
							if (tentG < existingGScore) {

								nodeParent.Remove(n);
								nodeParent.Add(n, currentNode);

								frontier.UpdatePriority(n, fsc);

								gScoresMap.Remove(n);
								gScoresMap.Add(n, tentG);
							}
						}
					}
				}

			} else if (searchMode == Mode.AStarVanilla) {
				
				float[] tentativeGScores = new float[(int)NodeDirections.Last + 1];
				
				// Directly adjacent g scores
				for (int i = 0; i < ((int)NodeDirections.Last + 1) / 2; i++) {
					tentativeGScores[i] = currentNodeGScore + grid.GetDivisionSize();
				}
				
				// Diagonally adjacent g scores
				for (int i = ((int)NodeDirections.Last + 1) / 2; i < ((int)NodeDirections.Last + 1); i++) {
					tentativeGScores[i] = currentNodeGScore + (float)(Math.Sqrt(2.0)) * grid.GetDivisionSize();
				}
				
				Node[] neighbours = new Node[(int)NodeDirections.Last + 1];
				float[] fScores = new float[(int)NodeDirections.Last + 1];
				
				for (int i = (int)NodeDirections.First; i <= (int)NodeDirections.Last; i++) {
					neighbours[i] = currentNode.GetNeighbours()[i];
					if (neighbours[i] != null) {
						fScores[i] = tentativeGScores[i] + DistanceFromGoal(neighbours[i].GetPosition());
					}
				}
				
				for (int i = 0; i < neighbours.Length; i++) {
					
					Node n = neighbours[i];

					// Some of the neighbours may have been blocked or off the grid
					if (n != null) {

						if (!grid.IsBlocked(n) && !closedSet.Contains(n)) {

							if (!openSet.Contains(n)) {

								nodeParent.Add(n, currentNode);
								gScoresMap.Add(n, tentativeGScores[i]);
								frontier.Add(new KeyValuePair<float, Node>(fScores[i], n));
								openSet.Add(n, n);

							} else {

								// Neighbour is already in the open set. If the tentative g score is lower
								// than the existing one, update the parent and existing f and g scores.
								float existingGScore = (float)(gScoresMap[n]);

								if (tentativeGScores[i] < existingGScore) {
									
									nodeParent.Remove(n);
									nodeParent.Add(n, currentNode);
									
									frontier.UpdatePriority(n, fScores[i]);
									
									gScoresMap.Remove(n);
									gScoresMap.Add(n, tentativeGScores[i]);
								}
							}
						}
					}
				}
			}
		}
		
		// Couldn't find a path (this may be ok, e.g. predators can't reach you)
		return null;
	}
	
	private Node jump(Grid g, Node currentArg, int directionXArg, int directionYArg, Node goal) {
		
		Stack nodeStack = new Stack();
		Stack dirXStack = new Stack();
		Stack dirYStack = new Stack();
		Stack consumeStack = new Stack();
		
		Stack result = new Stack();
		
		nodeStack.Push(currentArg);
		dirXStack.Push(directionXArg);
		dirYStack.Push(directionYArg);
		consumeStack.Push(false);
		
		// TO DO: Might be nice to fix all the gotos
		// Add a recursive version of this function as well
	MainLoopStart:
		while (nodeStack.Count > 0) {
			
			Node current = (Node)nodeStack.Pop();
			int directionX = (int)dirXStack.Pop();
			int directionY = (int)dirYStack.Pop();
			bool consume = (bool)consumeStack.Pop();
			
			Node nextNode = current.getNextNode(directionX, directionY);
			
			if (g.IsBlocked(nextNode)) {
				result.Push(null);
				goto MainLoopStart;
			}
			
			if (nextNode.Equals(goal)) {
				result.Push(nextNode);
				goto MainLoopStart;
			}
			
			Node offsetNode = nextNode;
			
			Node n1, n2, n3, n4;
			
			// Diagonal movement
			if (directionX != 0 && directionY != 0) {
				
				while (true) {
					
					// Diagonal Forced Neighbor Check
					n1 = offsetNode.getNextNode(-directionX, directionY);
					n2 = offsetNode.getNextNode(-directionX, 0);
					n3 = offsetNode.getNextNode(directionX, - directionY);
					n4 = offsetNode.getNextNode(0, - directionY);
					
					if (((!g.IsBlocked(n1)) && g.IsBlocked(n2))
					    || ((!g.IsBlocked(n3)) && g.IsBlocked(n4))) {
						
						result.Push(offsetNode);
						goto MainLoopStart;
					}
					
					// Check if we've diagonally moved to a point where we now need to go non-diagonally (if that makes sense...)
					if (consume) {
						
						// Don't consume again while we're in this loop!
						consume = false;
						
						Node result1 = (Node)result.Pop();
						Node result2 = (Node)result.Pop();
						
						if ((result1 != null) || (result2 != null)) {
							result.Push(offsetNode);
							goto MainLoopStart;
						}
						
					} else {
						// Ensure that we get back here once the recursive calls are done
						nodeStack.Push(current);
						dirXStack.Push(directionX);
						dirYStack.Push(directionY);
						consumeStack.Push(true);
						
						nodeStack.Push(offsetNode);
						dirXStack.Push(directionX);
						dirYStack.Push(0);
						consumeStack.Push(false);
						
						nodeStack.Push(offsetNode);
						dirXStack.Push(0);
						dirYStack.Push(directionY);
						consumeStack.Push(false);
						
						goto MainLoopStart;
					}
					
					/*
					// Alternative recursive call
					if ((jump(g, offsetNode, directionX, 0, goal) != null)
					    || (jump(g, offsetNode, 0, directionY, goal) != null)) {

						result.Push(offsetNode);
						goto MainLoopStart;
					}*/
					
					current = offsetNode;
					offsetNode = offsetNode.getNextNode(directionX, directionY);
					
					if (g.IsBlocked(offsetNode)) {
						result.Push(null);
						goto MainLoopStart;
					}
					
					if (offsetNode.Equals(goal)) {
						result.Push(offsetNode);
						goto MainLoopStart;
					}
				}
			} else {
				
				// Horizontal movement
				if (directionX != 0) {
					while (true) {
						
						// Diagonally up
						n1 = offsetNode.getNextNode(directionX, 1);
						
						// Up
						n2 = offsetNode.getNextNode(0, 1);
						
						// Diagonally down
						n3 = offsetNode.getNextNode(directionX, -1);
						
						// Down
						n4 = offsetNode.getNextNode(0, -1);
						
						if (((!g.IsBlocked(n1)) && g.IsBlocked(n2))
						    || ((!g.IsBlocked(n3)) && g.IsBlocked(n4))) {
							
							result.Push(offsetNode);
							goto MainLoopStart;
						}
						
						offsetNode = offsetNode.getNextNode(directionX, directionY);
						
						if (g.IsBlocked(offsetNode)) {
							result.Push(null);
							goto MainLoopStart;
						}
						
						if (offsetNode.Equals(goal)) {
							result.Push(offsetNode);
							goto MainLoopStart;
						}
					}
				}
				else {
					
					// Vertical movement
					while (true) {
						
						// Diagonally right
						n1 = offsetNode.getNextNode(1, directionY);
						
						// Right
						n2 = offsetNode.getNextNode(1, 0);
						
						// Diagonally left
						n3 = offsetNode.getNextNode(-1, directionY);
						
						// Left
						n4 = offsetNode.getNextNode(-1, 0);
						
						if (((!g.IsBlocked(n1)) && g.IsBlocked(n2))
						    || ((!g.IsBlocked(n3)) && g.IsBlocked(n4))) {
							
							result.Push(offsetNode);
							goto MainLoopStart;
						}
						
						offsetNode = offsetNode.getNextNode(directionX, directionY);
						
						if (g.IsBlocked(offsetNode)) {
							result.Push(null);
							goto MainLoopStart;
						}
						
						if (offsetNode.Equals(goal)) {
							result.Push(offsetNode);
							goto MainLoopStart;
						}
					}
				}
			}
		}
		
		return (Node)result.Pop();
	}
	
	private ArrayList identifySuccessors(Grid g, Node current, Node parent, Node goal) {
		
		ArrayList successors = new ArrayList();
		
		ArrayList neighbours = current.GetJPSNeighbours(g, parent);
		
		int directionX, directionY;
		
		foreach (object o in neighbours) {
			
			Node n = (Node)o;
			
			directionX = (int)Math.Round((n.GetPosition().x - current.GetPosition().x) / g.GetDivisionSize());
			directionY = (int)Math.Round((n.GetPosition().y - current.GetPosition().y) / g.GetDivisionSize());
			
			Node jumpPoint = jump(g, current, directionX, directionY, goal);
			
			if (jumpPoint != null) {
				successors.Add(jumpPoint);
			}
		}
		
		return successors;
	}
	
	public class FScoreComparer : IComparer  {
		
		private Hashtable fScores;
		
		public FScoreComparer(Hashtable fScores) {
			this.fScores = fScores;
		}
		
		int IComparer.Compare(object x, object y)  {
			return ((float)fScores[x]).CompareTo((float)fScores[y]);
		}
	}
}