using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour {

	public static AudioManager 		S;
	public AudioClip 				gameOver;
	public AudioClip 				gameWon;

	// Use this for initialization
	void Start () {
		if (S == null)
		{
			S = this;
		}
		else if (S != null)
		{
			Destroy (this);
		}

		S.GetComponent<AudioSource> ().Play ();
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
