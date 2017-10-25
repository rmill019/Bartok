using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour {
	[Header("Set Dynamically")]
	public string 				suit;					//Suit of the Card (C,D,H, or S)
	public int					rank;					//Rank of the Card (1-14)
	public Color				color = Color.black;	//Color to tint pips
	public string				colS = "Black";			// or "Red". Name of color

	//This List holds all of the Decorator GameObjects
	public List<GameObject> 	decoGOs = new List<GameObject>();

	//This list holds all of the Pip GameObjects
	public List<GameObject> 	pipGOs = new List<GameObject>();

	public GameObject			back; 					//The GameObject of the back of the card

	public CardDefinition		def;					//Parsed from DeckXML.xml

	public bool faceUp{
		get{ return(!back.activeSelf); }
		set{ back.SetActive (!value); }
	}

	//Virtual method can be overridden by subclass methods with the same name
	virtual public void OnMouseUpAsButton(){
		print (name);		//When clicked this outputs the card name
	}

	//List of the the SpriteRenderer Components of this GameObject and its children
	public SpriteRenderer[]		spriteRenderers;

	void Start(){
		SetSortOrder (0); //Ensures that the card stats properly depth sorted
	}

	//If spriteRenderers is not yet defined, this function defines it
	public void PopulateSpriteRenderers(){
		//If spriteRenderers is null or empty
		if (spriteRenderers == null || spriteRenderers.Length == 0) {
			//Get SpriteRenderer Components of this GameObject and its children
			spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
		}
	}

	//Sets the sortingLayerName on all SpriteRenderer Components
	public void SetSortingLayerName(string tSLN){
		PopulateSpriteRenderers ();

		foreach (SpriteRenderer tSR in spriteRenderers) {
			tSR.sortingLayerName = tSLN;
		}
	}

	public void SetSortOrder(int sOrd){
		PopulateSpriteRenderers ();

		//Iterate through all the spriteRenderers as tSR
		foreach (SpriteRenderer tSR in spriteRenderers) {
			//If the gameObject is this.gameObject, it's the background
			if (tSR.gameObject == this.gameObject) {
				//Set its order to sOrd
				tSR.sortingOrder = sOrd;
				//And continue to the next iteration of the loop
				continue; 
			}

			//Each of the children of this GameObject are named switch based on the names
			switch (tSR.gameObject.name) {
			case "back":
				//Set it to the highest layer to cover the other sprites
				tSR.sortingOrder = sOrd + 2;
				break;
			case "face":
			default:
				//Set it to the middle layer to be above the background
				tSR.sortingOrder = sOrd + 1;
				break;
			}
		}
	}
}

[System.Serializable] //A Serializable class is able to be edited in the Inspector
public class Decorator{
	//This class stores info about each decorator or pip from DeckXML
	public string			type;							//For card pips, type = "pip"
	public Vector3			loc;							//The location of the Sprite on the Card
	public bool				flip = false;					//Whether to flip the Sprite Vertically (not it is initialized to false)
	public float			scale = 1f;						//The scale of the Sprite
}

[System.Serializable]
public class CardDefinition {
	//This class stores information for each rank of card
	public string			face;							//Sprite to use for each face card
	public int				rank;							//The rank (1-13, corresponding to Ace through King) of each card
	public List<Decorator>	pips = new List<Decorator>();	//Pips used
}
