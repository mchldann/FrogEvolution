using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PriorityQueue<TPriority, TValue> {

	private List<KeyValuePair<TPriority, TValue>> data;
	private IComparer<TPriority> comparer;
	private Hashtable dataMap; // For quick searching of the heap by value. Maps values to their array position.
	
	public PriorityQueue()
	{
		data = new List<KeyValuePair<TPriority, TValue>>();
		comparer = Comparer<TPriority>.Default;
		dataMap = new Hashtable();
	}

	public int Count
	{
		get {
			if (data.Count == 0) {
				return 0;
			} else {
				return data.Count - 1;
			}
		}
	}

	public KeyValuePair<TPriority, TValue> GetElementByValue(TValue value) {

		if (dataMap.Contains(value)) {
			int arrayPos = (int)(dataMap[value]);
			return (KeyValuePair<TPriority, TValue>)data[arrayPos];
		} else {
			// TO DO: Error checking...
			return new KeyValuePair<TPriority, TValue>();
		}
	}

	public void UpdatePriority(TValue value, TPriority newPriority) {
		
		if (!dataMap.Contains(value)) {
			Debug.Log("ERROR - tried to update priority for value that doesn't exist!");
		}

		// Swap last node in tree with current node
		int foundPosition = (int)(dataMap[value]);

		if (foundPosition >= data.Count) {
			Debug.Log("ERROR: Data map violated! Data size is " + data.Count + ", position is " + foundPosition + ", map size is " + dataMap.Count);
		}

		KeyValuePair<TPriority, TValue> foundPair = data[foundPosition];
		KeyValuePair<TPriority, TValue> lastVal = data[data.Count - 1];

		data[foundPosition] = lastVal;

		dataMap.Remove(lastVal.Value);
		dataMap.Add(lastVal.Value, foundPosition);
		dataMap.Remove(foundPair.Value);

		data.RemoveAt(data.Count - 1);
		
		// Now we heapify this badboy...
		DownHeap(foundPosition);

		Add(new KeyValuePair<TPriority, TValue>(newPriority, foundPair.Value));
	}
	
	public void Add(KeyValuePair<TPriority, TValue> newElement) {
		
		// Push the element twice if the underlying vector has zero size so that we can treat the vector
		// as starting at index 1. (This makes the rest of the index calcs a lot easier.)
		if (data.Count == 0) {
			data.Add(new KeyValuePair<TPriority, TValue>());
		}

		data.Add(newElement);

		int currentPosition = Count;
		int parent = currentPosition / 2;

		dataMap.Add(newElement.Value, currentPosition);

		while (parent >= 1) {

			if (comparer.Compare(data[parent].Key, newElement.Key) > 0) {
				
				// Swap the current node with its parent
				KeyValuePair<TPriority, TValue> temp = data[currentPosition];
				data[currentPosition] = data[parent];
				data[parent] = temp;
				
				// Update the search map (TO DO: Ensure existence?)
				dataMap[data[currentPosition].Value] = currentPosition;
				dataMap[data[parent].Value] = parent;

				currentPosition = parent;
				parent = currentPosition / 2;
				
			} else {
				// We're done!
				break;
			}
		}
	}

	public KeyValuePair<TPriority, TValue> Dequeue() {

		// TO DO: Uh oh... What do we return if there's no first element???
		KeyValuePair<TPriority, TValue> returnPair = data[1];
		KeyValuePair<TPriority, TValue> lastVal = data[data.Count - 1];
		
		data[1] = lastVal;

		// Update the search map (TO DO: Ensure existence?)
		dataMap.Remove(returnPair.Value);
		
		// This was a bug in my C++ assignment!!
		if (Count != 1) {
			dataMap.Remove(lastVal.Value);
			dataMap.Add(lastVal.Value, 1);
		}
		
		data.RemoveAt(data.Count - 1);
		
		// Now we heapify this badboy...
		DownHeap(1);

		return returnPair;
	}

	public TValue DequeueValue() {

		KeyValuePair<TPriority, TValue> temp = Dequeue();
		return temp.Value;
	}

	public void DownHeap(int parent) {

		int child = parent * 2; // This actually just gives us the left child
		
		while (child <= Count) {
			
			// Check if there's a right child too
			if (child < Count) {
				
				// Take the right child if it is smaller
				if (comparer.Compare(data[child + 1].Key, data[child].Key) < 0) {
					child++;
				}
			}
			
			if (comparer.Compare(data[parent].Key, data[child].Key) > 0) {
				
				// Swap the parent with its child
				KeyValuePair<TPriority, TValue> temp = data[child];
				data[child] = data[parent];
				data[parent] = temp;
				
				// Update the search map (TO DO: Ensure existence?)
				dataMap[data[child].Value] = child;
				dataMap[data[parent].Value] = parent;
				
				parent = child;
				child = parent * 2;
				
			} else {
				// We're done!
				break;
			}
		}
	}

	// For debugging
	public void Print() {
		for (int i = 1; i < data.Count; i++) {
			Debug.Log("Element " + i + " has key " + data[i].Key);
		}
	}
}