using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Prospector : MonoBehaviour {
	static public Prospector	S;

	[Header("Set in Inspector")]
	public TextAsset 				deckXML;
	public TextAsset 				layoutXML;
	public float					xOffset = 3;
	public float 					yOffset = -2.5f;
	public Vector3					layoutCenter;
	public Vector2					fsPosMid = new Vector2 ( 0.5f, 0.90f);
	public Vector2					fsPosRun = new Vector2 ( 0.5f, 0.75f);
	public Vector2					fsPosMid2 = new Vector2 ( 0.4f, 1.0f);
	public Vector2					fsPosEnd = new Vector2 ( 0.5f, 0.95f);
	public float 					reloadDelay = 2f;	//2 sec delay between rounds
	public Text						gameOverText, roundResultText, highScoreText;

	[Header("Set Dynamically")]
	public Deck 					deck;
	public Layout					layout;
	public List<CardProspector>		drawPile;
	public Transform 				layoutAnchor;
	public CardProspector 			target;
	public List<CardProspector> 	tableau;
	public List<CardProspector> 	discardPile;
	public FloatingScore 			fsRun;

	void Awake(){
		S = this; //Set up a Singleton for Prospector
		SetUpUITexts();
	}

	void SetUpUITexts(){
		//Set up the HighScore UI Text
		GameObject go = GameObject.Find("HighScore");
		if (go != null) {
			highScoreText = go.GetComponent<Text> ();
		}
		int highScore = ScoreManager.HIGH_SCORE;
		string hScore = "High Score: " + Utils.AddCommasToNumber (highScore);
		go.GetComponent<Text> ().text = hScore;

		//Set up the UI Texts that show at the end of the round
		go = GameObject.Find ("GameOver");
		if (go != null) {
			gameOverText = go.GetComponent<Text> ();
		}

		go = GameObject.Find ("RoundResult");
		if (go != null) {
			roundResultText = go.GetComponent<Text> ();
		}

		//Make the end of round texts invisible
		ShowResultsUI(false);
	}

	void ShowResultsUI(bool show){
		gameOverText.gameObject.SetActive (show);
		roundResultText.gameObject.SetActive (show);
	}
		
	void Start () {
		Scoreboard.S.score = ScoreManager.SCORE;

		deck = GetComponent<Deck> ();	//Get the Deck
		deck.InitDeck(deckXML.text);	//Pass DeckXML to it
		Deck.Shuffle(ref deck.cards); 	//This shuffles the deck by reference, note that you also use the ref keyword when calling the function

//	This section commented out
//		Card c;
		//reposition cards on screen in their new shuffled order
//		for (int cNum = 0; cNum < deck.cards.Count; cNum++) {
//			c = deck.cards [cNum];
//			c.transform.localPosition = new Vector3( (cNum%13)*3, cNum/13*4, 0);
//		}

		layout = GetComponent<Layout> ();	//Get the Layout Component
		layout.ReadLayout(layoutXML.text);	//Pass LayoutXML to it
		drawPile = ConvertListCardsToListCardProspectors(deck.cards);
		LayoutGame();
	}

	List<CardProspector> ConvertListCardsToListCardProspectors(List<Card> lCD){
		List<CardProspector> lCP = new List<CardProspector> ();
		CardProspector tCP;
		foreach (Card tCD in lCD) {
			tCP = tCD as CardProspector;
			lCP.Add (tCP);
		}
		return (lCP);
	}

	//The Draw function will pull a single card from the drawPile and return it
	CardProspector Draw(){
		CardProspector cd = drawPile [0];	//pull the 0th CardProspector
		drawPile.RemoveAt(0);				//then remove it from List<> drawPile
		return(cd);							//And return it
	}

	//Layout Game() positions the intitial tableau of cards, a.k.a. the "mine"
	void LayoutGame(){
		//Create an empty GameObject to serve as an achor for the tableau 
		if (layoutAnchor == null) {
			//Create an empty GameObject named _LayoutAnchor in the Hierarchy
			GameObject tGO = new GameObject ("_LayoutAnchor");
			layoutAnchor = tGO.transform;						//Grab its Transform
			layoutAnchor.transform.position = layoutCenter; 	//Position it
		}

		CardProspector cp;
		//Follow the layout
		//Itertate through all the SlotDefs in the layout.slotDef as tSD
		foreach (SlotDef tSD in layout.slotDefs){
			cp = Draw ();							//Pull a card from the top (beginning) of the draw Pile
			cp.faceUp = tSD.faceUp;					//Set its faceUp to the value in SlotDef
			cp.transform.parent = layoutAnchor;		//Make its parent layoutAnchor
			//This replaces the previous parent: deckAnchor, which appears as _Deck in the Hierarchy when the scene is playing.

			//Set the localPosition of the card based on slotDef
			cp.transform.localPosition = new Vector3 (
				layout.multiplier.x * tSD.x,
				layout.multiplier.y * tSD.y,
				-tSD.layerID
			);
			cp.layoutID = tSD.id;
			cp.slotDef = tSD;
			//CardProspectors in the tableau have the state CardState.tableau
			cp.state = eCardState.tableau;
			//Set the sorting layers
			cp.SetSortingLayerName(tSD.layerName);


			tableau.Add (cp); 						//Add this CardProspector to the List<> tableau
		}

		//Set which cards are hiding others
		foreach (CardProspector tCP in tableau) {
			foreach (int hid in tCP.slotDef.hiddenBy) {
				cp = FindCardByLayoutID (hid);
				tCP.hiddenBy.Add (cp);
			}
		}

		//Set up the initial target card
		MoveToTarget(Draw());

		//Set up the Draw pile
		UpdateDrawPile ();

	} //END LayoutGame()

	//Convert from the layoutID int to the CardProspector with that ID
	CardProspector FindCardByLayoutID(int layoutID){
		foreach (CardProspector tCP in tableau) {
			//Search through all cards in the tableau List<>
			if (tCP.layoutID == layoutID) {
				//if the card has the same ID, return it
				return (tCP);
			}
		}
		//if it's not found, return null
		return (null);
	}

	void SetTableauFaces(){
		foreach (CardProspector cd in tableau) {
			bool faceUP = true; //Assume the card will be face-up
			foreach (CardProspector cover in cd.hiddenBy) {
				//If either of the covering cards are in the tableau
				if (cover.state == eCardState.tableau) {
					faceUP = false; // then this card is face-down
				}
			}
			cd.faceUp = faceUP; //Set the value on the card
		}
	}

	//Moves the current target to the discardPile
	void MoveToDiscard(CardProspector cd){
		//Set the state of the card to discard
		cd.state = eCardState.discard;
		discardPile.Add (cd);					//Add it to the discardPile List<>
		cd.transform.parent = layoutAnchor;		//Update its tranform parent

		//Position this card on the discardPile
		cd.transform.localPosition = new Vector3 (
			layout.multiplier.x * layout.discardPile.x,
			layout.multiplier.y * layout.discardPile.y,
			-layout.discardPile.layerID+0.5f
		);

		cd.faceUp = true;
		//Place it on top of the pile for depth sorting
		cd.SetSortingLayerName(layout.discardPile.layerName);
		cd.SetSortOrder (-100 + discardPile.Count);
	}

	//Make cd the new target card
	void MoveToTarget(CardProspector cd){
		//If there is currently a target card, move it to discardPile
		if (target != null) MoveToDiscard(target);
		target = cd; // cd is the new target
		cd.state = eCardState.target;
		cd.transform.parent = layoutAnchor;
		//Move to the target position
		cd.transform.localPosition = new Vector3 (
			layout.multiplier.x * layout.discardPile.x,
			layout.multiplier.y * layout.discardPile.y,
			-layout.discardPile.layerID
		);

		cd.faceUp = true; // Make it face-up
		//Set the depth sorting
		cd.SetSortingLayerName(layout.discardPile.layerName);
		cd.SetSortOrder (0);
	}

	//Aranges all the cards of the drawPile to show how many are left
	void UpdateDrawPile(){
		CardProspector cd;
		//Go through all the cards of the drawPile
		for (int i = 0; i < drawPile.Count; i++) {
			cd = drawPile [i];
			cd.transform.parent = layoutAnchor;

			//Position it correctly with the layout.drawPile.stagger
			Vector2 dpStagger = layout.drawPile.stagger;
			cd.transform.localPosition = new Vector3 (
				layout.multiplier.x * (layout.drawPile.x + i*dpStagger.x),
				layout.multiplier.y * (layout.drawPile.y + i*dpStagger.y),
				-layout.drawPile.layerID + 0.1f*i
			);

			cd.faceUp = false;
			cd.state = eCardState.drawpile;
			//Set depth sorting
			cd.SetSortingLayerName(layout.drawPile.layerName);
			cd.SetSortOrder (-10 * i);
		}
	}

	//CardClicked is called any time a card in the game is clicked
	public void CardClicked(CardProspector cd){
		//The reaction is determined by the state of the clicked card
		switch (cd.state) {
			case eCardState.target:
				//Clicking the target card does nothing
				break;
		case eCardState.drawpile:
				//Clicking any card in the drawPile will draw the next card
			MoveToDiscard (target);
			MoveToTarget (Draw ());
			UpdateDrawPile ();
				//Calls ScoreManager component
				ScoreManager.EVENT (eScoreEvent.draw);
				FloatingScoreHandler (eScoreEvent.draw);
				break;
		case eCardState.tableau:
					//Clicking a card in the tableau will check it it's a valid play
			bool validMatch = true;
			if (!cd.faceUp) {
				//If the card is face-down, it's not alid
				validMatch = false;
			}
			if (!AdjacentRank (cd, target)) {
				//if it's not an adjacent rank, it's not valid
				validMatch = false;
			}
			if (!validMatch)
				return; //return if not valid

				//If we got here, then it's a valid card
				tableau.Remove (cd); //Remove it from the tableau List
				MoveToTarget (cd); //Make it the target card
				SetTableauFaces ();
				ScoreManager.EVENT (eScoreEvent.mine);
				FloatingScoreHandler (eScoreEvent.mine);
				break;
		}
		//Check to see whether the game is over or not
		CheckForGameOver();
	}//END CardClicked()

	//Return true if the two cards are adjacent in rank (Ace & King wrap around)
	public bool AdjacentRank(CardProspector c0, CardProspector c1){
		//If either card is face-down, it's not adjacent
		if (!c0.faceUp || !c1.faceUp) return (false);

		//If they are 1 apart, they are adjacent
		if (Mathf.Abs(c0.rank - c1.rank) == 1) return (true);

		//If one is Ace and the other King, they are adjacent
		if (c0.rank == 1 && c1.rank	== 13) return (true);
		if (c0.rank == 13 && c1.rank == 1) return (true);
	
		//Otherwise, return false
		return (false);
	}//END AdjacentRank()

	//Test whether the game is over
	void CheckForGameOver(){
		//If the tableau is empty, the game is over
		if (tableau.Count == 0) {
			//Call GameOver() with a win
			GameOver(true);
			return;
		}

		//If there are still cards in the draw pile, the gqme's not over
		if (drawPile.Count > 0) {
			return;
		}

		//check for remaining valid plays
		foreach (CardProspector cd in tableau) {
			if (AdjacentRank (cd, target)) {
				//If there is a valid play, the game's not over
				return;
			}
		}

		//Since there are no valid plays, the game is over. Call GameOver with a loss
		GameOver(false);
	}

	//Called when the game is over.
	void GameOver(bool won){
		int score = ScoreManager.SCORE;
		if (fsRun != null) score += fsRun.score;
		if (won) {
//			print ("Game Over. You Won! :)");
			gameOverText.text = "Round Over";
			roundResultText.text = "You won this round! \nRound Score: " + score;
			ShowResultsUI (true);
			ScoreManager.EVENT(eScoreEvent.gameWin);
			FloatingScoreHandler (eScoreEvent.gameWin);
		} else {
//			print ("Game Over. You Lost. :(");
			gameOverText.text = "Game Over";
			if (ScoreManager.HIGH_SCORE <= score) {
				string str = "You got the high score!\nHigh score: " + score;
				roundResultText.text = str;
			} else {
				roundResultText.text = "Your final score was: " + score;
			}
			ShowResultsUI (true);
			ScoreManager.EVENT(eScoreEvent.gameLoss);
			FloatingScoreHandler (eScoreEvent.gameLoss);
		}
		//Reload the scene, resetting the game
//		SceneManager.LoadScene("__Prospector_Scene_0");

		//Reload the scene in reloadDelay seconds
		//This will give the score a moment to travel. 
			//The Invoke() command works by calling a function named "ReloadLevel" in reloadDelay seconds.
			//This is similar to how SendMessage() works, but it does so with a delay. Default delay is 2 seconds
		Invoke ( "ReloadLevel", reloadDelay);
	}

	void ReloadLevel(){
		//Reload the scene, resetting the game
		SceneManager.LoadScene("__Prospector_Scene_0");
	}

	//Handle FloatingScore movement
	void FloatingScoreHandler (eScoreEvent evt){
		List<Vector2> fsPts;
		switch (evt) {
			//Same things need to happen whether it's a draw, win, or a loss
		case eScoreEvent.draw:		//Drawing a card
		case eScoreEvent.gameWin:	//Won the round
		case eScoreEvent.gameLoss:	//Lost the round
			//Add fsRun to the Scoreboard score
			if (fsRun != null) {
				//Create points for the Bezier curve
				fsPts = new List<Vector2> ();
				fsPts.Add (fsPosRun);
				fsPts.Add (fsPosMid2);
				fsPts.Add (fsPosEnd);
				fsRun.reportFinishTo = Scoreboard.S.gameObject;
				fsRun.Init (fsPts, 0, 1);
				//Also adjust the fontSize
				fsRun.fontSizes = new List<float> (new float[] { 28, 36, 4 });
				fsRun = null;	//Clear fsRun so it's created again
			}
			break;
		case eScoreEvent.mine:	//Remove a mine card
			//Create a FloatingScore for this score
			FloatingScore fs;
			// Move it from the mousPosition to fsPosRun
			Vector2 p0 = Input.mousePosition;
			p0.x /= Screen.width;
			p0.y /= Screen.height;
			fsPts = new List<Vector2> ();
			fsPts.Add (p0);
			fsPts.Add (fsPosMid);
			fsPts.Add (fsPosRun);
			fs = Scoreboard.S.CreateFloatingScore (ScoreManager.CHAIN, fsPts);
			fs.fontSizes = new List<float> (new float[] { 4, 50, 28 });
			if (fsRun == null) {
				fsRun = fs;
				fsRun.reportFinishTo = null;
			} else {
				fs.reportFinishTo = fsRun.gameObject;
			}
			break;
		}
	}//END FloatingScoreHandler
}
