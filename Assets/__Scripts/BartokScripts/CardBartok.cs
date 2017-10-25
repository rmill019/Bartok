using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// CBState includes both states for the game and to____ states for movement
public enum CBState {
	drawPile,
	toHand,
	hand,
	toTarget,
	target,
	discard,
	to,
	idle
}

// CardBartok extends Card just as CardProspector did
public class CardBartok : Card {
	// These static fields are used to set values that will be the same for all instances of CardBartok
	static public float			MOVE_DURATION = 0.5f;
	static public string		MOVE_EASING = Easing.InOut;
	static public float			CARD_HEIGHT = 3.5f;
	static public float 		CARD_WIDTH = 2f;

	public CBState 				state = CBState.drawPile;

	// Fields to store info the card will use to move and rotate
	public List<Vector3>		bezierPts;
	public List<Quaternion> 	bezierRots;
	public float 				timeStart;
	public float 				timeDuration;

	// When the card is done moving it will call reportFinishTo.SendMessage()
	public GameObject			reportFinishTo = null;

	// MoveTo tells the card to interpolate to a new position and rotation
	public void MoveTo (Vector3 ePos, Quaternion eRot)
	{
		// Make new interpolation lists for the card.
		// Position and Rotation will each have only two points

		bezierPts = new List<Vector3> ();
		bezierPts.Add (transform.localPosition);		// Current position
		bezierPts.Add (ePos);							// New Position
		bezierRots = new List<Quaternion>();
		bezierRots.Add (transform.rotation);			// Current Rotation
		bezierRots.Add (eRot);							// New Rotation

		// if timeStart is 0, then it's set to start immediately,
		// otherwise, it starts at timeStart. This way, if timeStart is
		// already set, it won't be overwritten.
		if (timeStart == 0) 
		{
			timeStart = Time.time;
		}
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
