using UnityEngine;
using System.Collections;
using System;

public class Node {

	private static float DEBUG_DRAW_RADIUS = 0.4f;

	// Storing the actual row and column of the node in the grid (as opposed to just the
	// node's world position) is useful for testing equality and writing a nice hash function
	private int gridRow;
	private int gridColumn;

	// The world position of the node
	private Vector2 position;

	// The adjacency list
	private Node[] neighbours = new Node[(int)NodeDirections.Last + 1];

	// Getters
	public int GetGridRow() {
		return gridRow;
	}
	
	public int GetGridColumn() {
		return gridColumn;
	}
	
	public Vector2 GetPosition() {
		return position;
	}
	
	public Node[] GetNeighbours() {
		return neighbours;
	}

	// Setters
	public void SetNeighbours(Node[] neighbours) {
		this.neighbours = neighbours;
	}

	// We don't want to match nodes by reference in A*. If their posiitons are the same then they're
	// equal for our purposes.
	public override bool Equals(object other)
	{
		if (other == null)
			return false;
		else
			return ((gridRow == ((Node)other).GetGridRow()) && (gridColumn == ((Node)other).GetGridColumn()));
	}
	
	// Several examples on the net calculate the hash this way. It seems to work well.
	public override int GetHashCode() {
		
		unchecked // We don't care if the hash overflows
		{
			int hash = 17;
			hash = hash * 23 + gridRow;
			hash = hash * 23 + gridColumn;
			return hash;
		}
	}

	// Constructor
	public Node(int gridRow, int gridColumn, Vector2 position) {
		this.gridRow = gridRow;
		this.gridColumn = gridColumn;
		this.position = position;
	}

	// For drawing in an Update() method
	public void DebugDraw(float gridDivisionSize, Color color) {
		Debug.DrawLine(position + new Vector2(-DEBUG_DRAW_RADIUS, DEBUG_DRAW_RADIUS) * gridDivisionSize, position + new Vector2(DEBUG_DRAW_RADIUS, DEBUG_DRAW_RADIUS) * gridDivisionSize, color);
		Debug.DrawLine(position + new Vector2(DEBUG_DRAW_RADIUS, DEBUG_DRAW_RADIUS) * gridDivisionSize, position + new Vector2(DEBUG_DRAW_RADIUS, -DEBUG_DRAW_RADIUS) * gridDivisionSize, color);
		Debug.DrawLine(position + new Vector2(DEBUG_DRAW_RADIUS, -DEBUG_DRAW_RADIUS) * gridDivisionSize, position + new Vector2(-DEBUG_DRAW_RADIUS, -DEBUG_DRAW_RADIUS) * gridDivisionSize, color);
		Debug.DrawLine(position + new Vector2(-DEBUG_DRAW_RADIUS, -DEBUG_DRAW_RADIUS) * gridDivisionSize, position + new Vector2(-DEBUG_DRAW_RADIUS, DEBUG_DRAW_RADIUS) * gridDivisionSize, color);
	}

	// This method is used by JPS. It just returns the neighbouring node in the direction we're travelling.
	public Node getNextNode(int directionX, int directionY) {
		
		Node nextNode = null;
		
		if (directionX == -1) {
			if (directionY == -1) {
				nextNode = neighbours[(int)NodeDirections.BottomLeft];
			} else if (directionY == 0) {
				nextNode = neighbours[(int)NodeDirections.Left];
			} else if (directionY == 1) {
				nextNode = neighbours[(int)NodeDirections.TopLeft];
			}
		} else if (directionX == 0) {
			if (directionY == -1) {
				nextNode = neighbours[(int)NodeDirections.Bottom];
			} else if (directionY == 0) {
				Debug.Log("WARNING: Node passed itself to getNextNode!");
				return this;
			} else if (directionY == 1) {
				nextNode = neighbours[(int)NodeDirections.Top];
			}
		} else if (directionX == 1) {
			if (directionY == -1) {
				nextNode = neighbours[(int)NodeDirections.BottomRight];
			} else if (directionY == 0) {
				nextNode = neighbours[(int)NodeDirections.Right];
			} else if (directionY == 1) {
				nextNode = neighbours[(int)NodeDirections.TopRight];
			}
		}
		
		return nextNode;
	}

	// By using the position of the parent node, we can determine which direction we're travelling in.
	// When we expand the current node, we can intelligently ignore some neighbours by taking into account
	// the current direction. This method returns the neighbours that we DO have to consider.
	public ArrayList GetJPSNeighbours(Grid g, Node parent) {
		
		ArrayList result = new ArrayList();
		
		// If we're at the start node then just return all neighbours
		if (parent == null) {
			foreach (Node n in neighbours) {
				if (!g.IsBlocked(n)) {
					result.Add(n);
				}
			}
			return result;
		}

		// Determine direction of travel
		int directionX = (int)Math.Round((position.x - parent.GetPosition().x) / g.GetDivisionSize());
		int directionY = (int)Math.Round((position.y - parent.GetPosition().y) / g.GetDivisionSize());

		// Set up different types of neighbouring nodes, based on the direction we're travelling
		Node directionXNode = null, directionYNode = null, reverseXNode = null, reverseYNode = null, directionYRightNode = null, directionYLeftNode = null, directionXTopNode = null, directionXBottomNode = null;
		
		if (directionX > 0) {
			directionXNode = neighbours[(int)NodeDirections.Right];
			reverseXNode = neighbours[(int)NodeDirections.Left];
			directionXTopNode = neighbours[(int)NodeDirections.TopRight];
			directionXBottomNode = neighbours[(int)NodeDirections.BottomRight];
		} else if (directionX < 0) {
			directionXNode = neighbours[(int)NodeDirections.Left];
			reverseXNode = neighbours[(int)NodeDirections.Right];
			directionXTopNode = neighbours[(int)NodeDirections.TopLeft];
			directionXBottomNode = neighbours[(int)NodeDirections.BottomLeft];
		}
		
		if (directionY > 0) {
			directionYNode = neighbours[(int)NodeDirections.Top];
			reverseYNode = neighbours[(int)NodeDirections.Bottom];
			directionYRightNode = neighbours[(int)NodeDirections.TopRight];
			directionYLeftNode = neighbours[(int)NodeDirections.TopLeft];
		} else if (directionY < 0) {
			directionYNode = neighbours[(int)NodeDirections.Bottom];
			reverseYNode = neighbours[(int)NodeDirections.Top];
			directionYRightNode = neighbours[(int)NodeDirections.BottomRight];
			directionYLeftNode = neighbours[(int)NodeDirections.BottomLeft];
		}
		
		// Diagonal movement
		if (directionX != 0 && directionY != 0) {
			
			Node directionDiagNode = null, reverseXForwardYNode = null, reverseYForwardXNode = null;
			
			if ((directionX > 0) && (directionY > 0)) {
				directionDiagNode = neighbours[(int)NodeDirections.TopRight];
				reverseXForwardYNode = neighbours[(int)NodeDirections.TopLeft];
				reverseYForwardXNode = neighbours[(int)NodeDirections.BottomRight];
			} else if ((directionX > 0) && (directionY < 0)) {
				directionDiagNode = neighbours[(int)NodeDirections.BottomRight];
				reverseXForwardYNode = neighbours[(int)NodeDirections.BottomLeft];
				reverseYForwardXNode = neighbours[(int)NodeDirections.TopRight];
			} else if ((directionX < 0) && (directionY > 0)) {
				directionDiagNode = neighbours[(int)NodeDirections.TopLeft];
				reverseXForwardYNode = neighbours[(int)NodeDirections.TopRight];
				reverseYForwardXNode = neighbours[(int)NodeDirections.BottomLeft];
			} else if ((directionX < 0) && (directionY < 0)) {
				directionDiagNode = neighbours[(int)NodeDirections.BottomLeft];
				reverseXForwardYNode = neighbours[(int)NodeDirections.BottomRight];
				reverseYForwardXNode = neighbours[(int)NodeDirections.TopLeft];
			}
			
			// For diagonal movement we have to try moving up/down & left/right as well, since we
			// may have now moved in line with the goal. This is not the case for non-diagonal movement
			// (i.e. we don't have to check diagonals) because if moving diagonally was best then we
			// would have done it on the previous step, but we didn't.

			// Left/right
			if (!g.IsBlocked(directionXNode)) {
				result.Add(directionXNode);
			}
			
			// Up/down
			if (!g.IsBlocked(directionYNode)) {
				result.Add(directionYNode);
			}
			
			// Diagonal
			if ((!g.IsBlocked(directionXNode)) || (!g.IsBlocked(directionYNode))) {
				if (!g.IsBlocked(directionDiagNode)) {
					result.Add(directionDiagNode);
				}
			}
			
			// Forced neighbours
			if ((g.IsBlocked(reverseXNode)) && (!g.IsBlocked(directionYNode))) {
				if (!g.IsBlocked(reverseXForwardYNode)) {
					result.Add(reverseXForwardYNode);
				}
			}
			if ((g.IsBlocked(reverseYNode)) && (!g.IsBlocked(directionXNode))) {
				if (!g.IsBlocked(reverseYForwardXNode)) {
					result.Add(reverseYForwardXNode);
				}
			}
		}
		else
		{
			// Vertical movement
			if (directionX == 0) {
				
				if (!g.IsBlocked(directionYNode)) {
					
					result.Add(directionYNode);
					
					if (g.IsBlocked(neighbours[(int)NodeDirections.Right])) {
						if (!g.IsBlocked(directionYRightNode)) {
							result.Add(directionYRightNode);
						}
					}
					
					if (g.IsBlocked(neighbours[(int)NodeDirections.Left])) {
						if (!g.IsBlocked(directionYLeftNode)) {
							result.Add(directionYLeftNode);
						}
					}
				}
			}
			// Horizontal movement
			else {
				if (!g.IsBlocked(directionXNode)) {
					
					result.Add(directionXNode);
					
					if (g.IsBlocked(neighbours[(int)NodeDirections.Top])) {
						if (!g.IsBlocked(directionXTopNode)) {
							result.Add(directionXTopNode);
						}
					}
					
					if (g.IsBlocked(neighbours[(int)NodeDirections.Bottom])) {
						if (!g.IsBlocked(directionXBottomNode)) {
							result.Add(directionXBottomNode);
						}
					}
				}
			}
		}
		
		return result;
	}
}
