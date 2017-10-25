using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour {
	[Header("Set in Inspector")]
	public bool							startFaceUp = false;
	//Suits
	public Sprite 						suitClub;
	public Sprite 						suitDiamond;
	public Sprite 						suitHeart;
	public Sprite 						suitSpade;

	public Sprite[] 					faceSprites;
	public Sprite[] 					rankSprites;

	public Sprite 						cardBack;
	public Sprite 						cardBackGold;
	public Sprite 						cardFront;
	public Sprite 						cardFrontGold;

	//Prefabs
	public GameObject 					prefabCard;
	public GameObject 					prefabSprite;

	[Header("Set Dynamically")]
	public PT_XMLReader					xmlr;
	public List<string> 				cardNames;
	public List<Card> 					cards;
	public List<Decorator>				decorators;
	public List<CardDefinition> 		cardDefs;
	public Transform 					deckAnchor;
	public Dictionary<string,Sprite>	dictSuits;

	//These private variables will be reused several times in helper methods
	private Sprite						_tSp = null;
	private GameObject					_tGO = null;
	private SpriteRenderer				_tSR = null;


	//InitDeck is called by Propsector when it is ready
	public void InitDeck (string deckXMLText){
		//This creates an anchor for all the Card GameObjects in the Hierarchy
		if (GameObject.Find ("_Deck") == null) {
			GameObject anchorGO = new GameObject ("_Deck");
			deckAnchor = anchorGO.transform;
		}

		//Initialize the Dictionary of SuitSprites with necessary Sprites
		dictSuits = new Dictionary<string, Sprite> () {
			{ "C", suitClub },
			{ "D", suitDiamond },
			{ "H", suitHeart },
			{ "S", suitSpade }

		};

		//parse XML file into Card Definitions
		ReadDeck (deckXMLText);

		MakeCards();
	}

	//ReadDeck parses the XML file, passed to it, into CardDefinitions
	public void ReadDeck (string deckXMLText){
		xmlr = new PT_XMLReader ();			//create a new PT_XMLReader
		xmlr.Parse(deckXMLText);			//Use that PT_XMLReader to parse DeckXML

		//this prints a test line to show how xmlr can be used
		string s = "xml[0] decorator[0]";
		s += " type=" + xmlr.xml ["xml"] [0] ["decorator"] [0].att ("type");
		s += " x=" + xmlr.xml ["xml"] [0] ["decorator"] [0].att ("x");
		s += " y=" + xmlr.xml ["xml"] [0] ["decorator"] [0].att ("y");
		s += " scale=" + xmlr.xml ["xml"] [0] ["decorator"] [0].att ("scale");
//		print (s);

		//Read decorators for all Cards
		decorators = new List<Decorator>(); //Init the List of Decorators
		//Grab an PT_XMLHashList of all <decorator>s in the XML file
		PT_XMLHashList xDecos = xmlr.xml["xml"][0]["decorator"];
		Decorator deco;

		//for each <decorator> in the XML
		for (int i = 0; i < xDecos.Count; i++) {
			deco = new Decorator ();	//Make a new Decorator
			//Copy the attributes of the <decorator> to the new Decorator
			deco.type = xDecos[i].att("type");
			deco.flip = (xDecos [i].att ("flip") == "1"); //bool deco.flip is true if the text of the flip attribute is "1"
			deco.scale = float.Parse(xDecos[i].att ("scale")); //float needs to be parsed from the attribute strings before it can be stored to scale
			//modify Vector3 loc from its initial position of 0,0,0
			deco.loc.x = float.Parse(xDecos[i].att ("x"));
			deco.loc.y = float.Parse(xDecos[i].att ("y"));
			deco.loc.z = float.Parse(xDecos[i].att ("z"));
			//add the temporary deco to the List decorators
			decorators.Add (deco);
		}

		//Read pip locations for each card number
		cardDefs = new List<CardDefinition>(); //Init the List of Cards
		//Grab a PT_XMLHashList of all the <card>s in the XML file
		PT_XMLHashList xCardDefs = xmlr.xml["xml"][0]["card"];

		//for each of the <card>s
		for (int i = 0; i < xCardDefs.Count; i++) {
			//Create a new Card Definition
			CardDefinition cDef = new CardDefinition();
			//Parse the attribute values and add them to cDef
			cDef.rank = int.Parse (xCardDefs[i].att("rank"));
			//Grab an PT_XMLHashList of all the <pip>s on this <card>
			PT_XMLHashList xPips = xCardDefs[i]["pip"];
			if (xPips != null) {
				for (int j = 0; j < xPips.Count; j++) {
					//Iterate through all the <pip>s
					deco = new Decorator();
					//<pip>s on the <card> are handled via the Decorator Class
					deco.type = "pip";
					deco.flip = (xPips[j].att ("flip") == "1");
					deco.loc.x = float.Parse (xPips[j].att ("x"));
					deco.loc.y = float.Parse (xPips[j].att ("y"));
					deco.loc.z = float.Parse (xPips[j].att ("z"));
					if (xPips [j].HasAtt ("scale")) {
						deco.scale = float.Parse (xPips[j].att ("scale"));
					}
					cDef.pips.Add(deco);
				}
			}
			//Face cards (Jack, Queen and King) have a face attribute
			if (xCardDefs [i].HasAtt ("face")) {
				//cDef.face is the base name of the face card Sprite. So FaceCard_11 is the base name for the Jack faces Sprites, and the Jack of Clubs is FaceCard_11C
				cDef.face = xCardDefs [i].att ("face");
			}
			cardDefs.Add (cDef);
		}
	} //End ReadDeck()

	//Get the proper CardDefinition based on Rank (1-14 / Ace - King)
	public CardDefinition GetCardDefinitionByRank (int rnk) {
		//Search througth all of the CardDefinitions
		foreach (CardDefinition cd in cardDefs) {
			//If the rank is correct, return this definition
			if (cd.rank == rnk) {
				return (cd);
			}
		}
		return (null);
	}

	//Make the Card GameObjects
	public void MakeCards(){
		//cardNames will be the names of cards to build
		//Each suit goes from 1 to 14 (e.g. C1 to C14 for Clubs)
		cardNames = new List<string>();
		string[] letters = new string[] { "C", "D", "H", "S" };
		foreach (string s in letters) {
			for (int i = 0; i < 13; i++) {
				cardNames.Add (s + (i + 1));
			}
		}

		//Make a List to Hold all the cards
		cards = new List<Card>();

		//Iterate through all of the card names that were just made
		for (int i = 0; i < cardNames.Count; i++) {
			//Make the card and add it to the cards Deck
			cards.Add (MakeCard(i));
		}
	}

	//MakeCard(), AddDecorator(), AddPips(), AddFace() and GetFace() are private helper methods that chain together to create the cards
	private Card MakeCard(int cNum){ 
		//Create a new Card GameObject
		GameObject cgo = Instantiate(prefabCard) as GameObject;

		//set the transform.parent of the new card to the anchor
		cgo.transform.parent = deckAnchor;
		Card card = cgo.GetComponent<Card> (); // Get the Card Component

		//This line stacks the cards so that they're all in nice rows
		cgo.transform.localPosition = new Vector3( (cNum%13)*3, cNum/13*4, 0);  //uses the card number to determine X and Y position

		//Assign basic values to the Card
		card.name = cardNames[cNum];
		card.suit = card.name[0].ToString ();
		card.rank = int.Parse (card.name.Substring (1));
		//if Diamonds or Hearts make red
		if (card.suit == "D" || card.suit == "H") {
			card.colS = "Red";
			card.color = Color.red;
		}
		//Pull the CardDefinition for this card
		card.def = GetCardDefinitionByRank(card.rank);

		//calls it's helper classes
		AddDecorators (card);
		AddPips (card);
		AddFace (card);
		AddBack (card);

		return card;
			
	}

	private void AddDecorators(Card card){
		//Add Decorators
		foreach (Decorator deco in decorators) {
			if (deco.type == "suit") {
				//Instantiate a Sprite GameObject
				_tGO = Instantiate (prefabSprite) as GameObject;
				//Get the Sprite Renderer Component
				_tSR = _tGO.GetComponent<SpriteRenderer> ();
				//Set the Sprite to the proper suit
				_tSR.sprite = dictSuits [card.suit];
			}else {
				_tGO = Instantiate (prefabSprite) as GameObject;
				_tSR = _tGO.GetComponent<SpriteRenderer> ();
				//Get the proper Sprite to show this rank
				_tSp = rankSprites [ card.rank ];
				//Assign this rank Sprite to the SpriteRenderer
				_tSR.sprite = _tSp;
				//Set the color of the rank to match the suit
				_tSR.color = card.color;

			}

			//Make the deco Sprites render above the Card
			_tSR.sortingOrder = 1;
			//Make the decorator Sprite a child of the Card
			_tGO.transform.SetParent(card.transform);
			//set the localPosition based on the location from DeckXML
			_tGO.transform.localPosition = deco.loc;

			//Flip the decorator if needed
			if (deco.flip) {
				//An Euler rotation of 180 around the z axis will flip it
				_tGO.transform.rotation = Quaternion.Euler (0, 0, 180);
			}

			//Set the scale to keep decos from being too big
			if (deco.scale != 1){
				_tGO.transform.localScale = Vector3.one * deco.scale;
			}

			//Name this GameObject so it's easy to see
			_tGO.name = deco.type;

			//Add this deco GameObject to the List card.decoGOs
			card.decoGOs.Add(_tGO);
		}
	}// END OF AddDecorators

	private void AddPips(Card card){
		//For each of the pips in the definition...
		foreach (Decorator pip in card.def.pips) {
			//Instantiate a Sprite GameObject
			_tGO = Instantiate (prefabSprite) as GameObject;
			//Set the parent to be the card GameObject
			_tGO.transform.SetParent(card.transform);
			//Set the position to that specified in the XML
			_tGO.transform.localPosition = pip.loc;
			//Flip it if necessary
			if (pip.flip) {
				_tGO.transform.localScale = Vector3.one * pip.scale;
			}
			//Give this GameObject a name
			_tGO.name = "pip";
			//Get the SpriteRenderer Component
			_tSR = _tGO.GetComponent<SpriteRenderer>();
			//Set the Sprite to the proper suit
			_tSR.sprite = dictSuits[card.suit];
			//Set sortingOrder so the pip is rendered above the Card_Front
			_tSR.sortingOrder = 1;
			//Add this to the Card's list of pips
			card.pipGOs.Add(_tGO);
		}
	}

	private void AddFace(Card card){
		// No need to run if this isn't a face card
		if (card.def.face == "") {
			return; 
		}

		_tGO = Instantiate (prefabSprite) as GameObject;
		_tSR = _tGO.GetComponent<SpriteRenderer> ();
		//Generate the right name and pass it to GetFace()
		_tSp = GetFace( card.def.face+card.suit);
		//assign this Sprite to _tSR
		_tSR.sprite = _tSp;
		//Set the sortingOrder
		_tSR.sortingOrder = 1;
		_tGO.transform.SetParent (card.transform);
		_tGO.transform.localPosition = Vector3.zero;
		_tGO.name = "face";
	}

	//Find the proper face card Sprite
	private Sprite GetFace (string faceS){
		foreach (Sprite _tSP in faceSprites) {
			//If this Sprite has the right name
			if (_tSP.name == faceS) {
				//then return the Sprite
				return ( _tSP);
			}
		}
		//if nothing can be found return null
		return (null);
	}

	private void AddBack (Card card){
		//Add Card Back
		//The Card_Back will be able to cover everything else on the Card
		_tGO = Instantiate (prefabSprite) as GameObject;
		_tSR = _tGO.GetComponent<SpriteRenderer> ();
		_tSR.sprite = cardBack;
		_tGO.transform.SetParent (card.transform);
		_tGO.transform.localPosition = Vector3.zero;
		//This is a higher sortingOrder than anything else
		_tSR.sortingOrder = 2;
		_tGO.name = "back";
		card.back = _tGO;
		//Default to face up
		card.faceUp = startFaceUp; //Use the property faceUp of Card
	}

	//Shuffle the Cards in Deck.cards
		//The ref keyword is used to make sure that the List<Card> that is passed to List<Card> oCards is passed as a reference rather than copied into oCards.
		//This mean that anything that happens to 0Cards is actually happening to the variable that is passed in.
		//i.e. the cards of Deck will be shuffled without requiring a rturn variable
	static public void Shuffle (ref List<Card> oCards){
		//Create a temporary List to hold the new shuffle order
		List<Card> tCards = new List<Card>();

		int ndx; // This will hold the index of the card to be moved
		tCards = new List<Card>(); //Initialize the temporary List
		//Repeat as long as there are cards in the originial List
		while (oCards.Count > 0) {
			//Pick the index of a random card
			ndx = Random.Range (0,oCards.Count);
			//Add that card to the temporary List
			tCards.Add (oCards[ndx]);
			//And remove that card from the original List
			oCards.RemoveAt(ndx);
		}
		//Replace the original List with the temporary list
		oCards = tCards;
		//Because oCards is a reference (ref) parameter, the original argument that was passes in is changed as well

	}
}
