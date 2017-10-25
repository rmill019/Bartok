using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//An enum to handle all the possible scoring events
public enum eScoreEvent{
	draw,
	mine,
	mineGold,
	gameWin,
	gameLoss
}

//Score Manager handles all of the scoring
//This was part of the Prospector class in the old prototype design, but now is it's own reusable component
public class ScoreManager : MonoBehaviour {
	static private ScoreManager S;					//Private version of the singleton pattern (for protection)

	static public int 	SCORE_FROM_PREV_ROUND = 0;
	static public int 	HIGH_SCORE = 0;

	[Header ("Set Dynamically")]
	//Field to track score info
	public int			chain = 0;
	public int			scoreRun = 0;
	public int			score = 0;

	void Awake (){
		if (S == null) {
			S = this; //Set the private singleton
			//This more complex singleton assignment makes sure that an error is thrown if two different ScoreMangter instances try to assert themselves as the singleton S.
		} else {
			Debug.LogError ("Error: ScoreMAnager.Awake(): Singleton S is already set!");
		}

		//Check for a high score in PlayerPrefs
		if (PlayerPrefs.HasKey ("ProspectorHighScore")) {
			HIGH_SCORE = PlayerPrefs.GetInt ("ProspectorHighScore");
		}
		//Add the score from last round, which will be >0 if it was a win
		score += SCORE_FROM_PREV_ROUND;
		//And reset the SCORE_FROM_PREV_ROUND
		SCORE_FROM_PREV_ROUND = 0;
	}


	//The static public version of the EVENT() mehtod enables other classes (like Prospector) to send eScoreEvents to the ScoreManager class.
	//When they do so, EVENT() calls the public, non-static Event() method on the ScoreManager private singleton S. 
	//The try-catch clause here will alert you if EVENT() is called while S is still null
	static public void EVENT (eScoreEvent evt){
		try { //try-catch stops an eror from breaking your program
			S.Event(evt);
		}
		catch(System.NullReferenceException nre){
			Debug.LogError ("ScoreManager: EVENT() called while S=null.\n" + nre);
		}
	}

	void Event (eScoreEvent evt){
		switch (evt) {
			//Same things need to happen whether it's a draw, a win, or a loss
			case eScoreEvent.draw: 			//Drawing a card
			case eScoreEvent.gameWin:		//won the round
			case eScoreEvent.gameLoss:		//lost the round
				chain = 0;					//resets the score chain
				score += scoreRun;			//add scoreRun to total score
				scoreRun = 0;				//reset scoreRun
				break;
			case eScoreEvent.mine:			//Remove a mine card
				chain++;					//increase the score chain
				scoreRun += chain;			//add score for this card to run
				break;
		}

		//This second switch statement handles round wins and losses
		switch (evt) {
			case eScoreEvent.gameWin:
				//If it's a win, add the score to the next round
				//static fields are NOT reset by SceneMAnager.LoadScene()
				SCORE_FROM_PREV_ROUND = score;
				print ("You won this round! Round score: " + score);
				break;
			case eScoreEvent.gameLoss:
				//If it's a loss, check against the high score
				if (HIGH_SCORE <= score) {
					print ("You got the high score! High score: " + score);
					HIGH_SCORE = score;
					PlayerPrefs.SetInt ("ProspectorHighScore", score);
				} else {
					print ("Your final score for the game was: " + score);
				}
				break;
			default:
				print ("score: " + score + " scoreRun: " + scoreRun + " chain: " + chain);
				break;
		}
	}

	//These static properties allow read-only access to the public fields of the private ScoreManager singleton S
	static public int CHAIN { get { return S.chain; } }
	static public int SCORE { get { return S.score; } }
	static public int SCORE_RUN { get { return S.scoreRun; } }
}
