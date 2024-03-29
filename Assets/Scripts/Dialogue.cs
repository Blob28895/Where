using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;
using static Dialogue;
using static UnityEngine.ParticleSystem;
using UnityEngine.SceneManagement;

public class Dialogue : MonoBehaviour
{
	[System.Serializable]
	public enum Speaker
	{
		player,
		enemy
	}

	[System.Serializable]
	public class DialogueLine
	{
		public Speaker speaker;
		[TextArea(3,3)]
		public string line;
	}

	private int currentLine = 0;
	private IEnumerator typing;
	private bool currentlyTyping = false;
	private string currentMessage;
	private bool isTalking = false;
	private bool dialogueFinished = false; //Im putting this in here for my Acerola jam submission but if you ever want to be able to talk to something multiple times without reloading the scene this will break it
	private PlayerMovement player;
	private int lastEnemyLineIndex = -1;
	private AudioController ac;

	[Header("References")]
	[SerializeField] private ParticleSystem particles;
	[SerializeField] private SpriteRenderer spriteRenderer;
	[SerializeField] private GameObject playerDialogueBox;
	[SerializeField] private TextMeshProUGUI playerMessage;
	[SerializeField] private GameObject enemyDialogueBox;
	[SerializeField] private TextMeshProUGUI enemyMessage;
	[SerializeField] private AudioClip dialogueSound;

	[Header("Dialogue Content")]
	[Tooltip("Time in seconds between each character appearing on the screen. The character after a period will come out at double this speed.")]
	[SerializeField] private float typingSpeed = 0.02f;
	[SerializeField] DialogueLine[] lines;


	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.CompareTag("Player") && spriteRenderer.enabled)
		{
			if(lines.Length == 0)
			{
				StartCoroutine(flee());
				return;
			}
			if (dialogueFinished) { return; }
			player = collision.gameObject.GetComponent<PlayerMovement>();
			player.setCanMove(false);
			isTalking = true;
			nextLine();
		}
	}

	private void Update()
	{
		if (!isTalking ) { return; }

		if (Input.GetKeyDown(KeyCode.Space))
		{
			nextLine();
			ac.playSoundScript(dialogueSound, 0.1f);
		}
	}

	private void Awake()
	{
		ac = GameObject.FindGameObjectWithTag("Player").GetComponent<AudioController>();
		particles.Stop();
		if (lines.Length > 0)
		{
			for(int i = 0; i < lines.Length; ++i)
			{
				if (lines[i].speaker == Speaker.enemy)
				{
					lastEnemyLineIndex = i;
				}
			}
		}
	}

	// Start is called before the first frame update
	void Start()
	{
		enemyDialogueBox.SetActive(false);
		playerDialogueBox.SetActive(false);
	}

	public void Speak(DialogueLine line)
	{
		#region read DialogueLine
		GameObject speakerDialogueBox;
		GameObject listenerDialogueBox;
		TextMeshProUGUI speakerMessage;
		string message = line.line;
		if(line.speaker == Speaker.enemy)
		{
			speakerDialogueBox = enemyDialogueBox;
			speakerMessage = enemyMessage;
			listenerDialogueBox = playerDialogueBox;
		}
		else
		{
			speakerDialogueBox = playerDialogueBox;
			speakerMessage = playerMessage;
			listenerDialogueBox = enemyDialogueBox;
		}
		#endregion

		//if (PauseManager.isPaused) { return; }
		if (currentMessage == message && currentlyTyping) // if we are trying to say something that is already being said finish it
		{
			StopCoroutine(typing);
			currentlyTyping = false;
			//SoundManager.PlaySound(SoundManager.Sound.DialogueSound);
			speakerMessage.text = message;
			return;
		}
		else if (speakerMessage.text == message)
		{ // if we are trying to say something thats already on screen then close the dialogue
			if(currentLine == lines.Length - 1)
			{
				closeDialogue(line);
				dialogueFinished = true;
			}
			else
			{
				if(currentLine == lastEnemyLineIndex)
				{
					StartCoroutine(flee());
				}
				currentLine++;
				Speak(lines[currentLine]);
			}
			return;
		}
		else // otherwise that means that we are trying to say something that isnt already on the screen,  so type as usual
		{
			speakerMessage.text = "";
			speakerDialogueBox.SetActive(true);
			listenerDialogueBox.SetActive(false);
			//SoundManager.PlaySound(SoundManager.Sound.DialogueSound);
			typing = Type(speakerMessage, message);
			StartCoroutine(typing);
		}
	}

	public void closeDialogue(DialogueLine line)
	{
		#region read DialogueLine
		GameObject speakerDialogueBox;
		TextMeshProUGUI speakerMessage;
		string message = line.line;
		if (line.speaker == Speaker.enemy)
		{
			speakerDialogueBox = enemyDialogueBox;
			speakerMessage = enemyMessage;
		}
		else
		{
			speakerDialogueBox = playerDialogueBox;
			speakerMessage = playerMessage;
		}
		#endregion


		player.setCanMove(true);
		if (speakerDialogueBox.activeInHierarchy)
		{// this gets called when players leave the range of something they can interact with, so I dont want the sound to play if they just walk by without pulling up the dialogue box
		 //SoundManager.PlaySound(SoundManager.Sound.DialogueSound);
		}
		speakerDialogueBox.SetActive(false);
		if (currentlyTyping)
		{
			StopCoroutine(typing);
			currentlyTyping = false;
		}
		speakerMessage.text = "";
		isTalking = false;
		dialogueFinished = true;
		//StartCoroutine(flee());

		//This if statement and function is specific to "Where?"
		if (SceneManager.GetActiveScene().name == "Level 4")
		{
			GetComponent<ReverseRun>().StartReverseRun();
			//GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>().growWings();
		}
		if (SceneManager.GetActiveScene().name == "Level 1" && PlayerProgression.canDoubleJump)
		{
			Debug.Log("Getting in here");
			endGame();
		}
	}
		IEnumerator Type(TextMeshProUGUI textDisplay, string sentence)
	{
		currentlyTyping = true;
		currentMessage = sentence;
		foreach (char letter in sentence.ToCharArray())
		{
			textDisplay.text += letter;
			if(letter == '.' || letter == '?')
			{
				yield return new WaitForSeconds(typingSpeed * 2);
			}
			else
			{
				yield return new WaitForSeconds(typingSpeed);
			}
		}
		currentlyTyping = false;

	}

	public string getCurrentMessage()
	{
		return currentMessage;
	}

	public void nextLine()
	{
		Speak(lines[currentLine]);
	}

	private IEnumerator flee()
	{
		particles.Play();
		spriteRenderer.enabled = false;
		yield return new WaitForSeconds(0.5f);
		particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
	}

	private void endGame()
	{
		GameObject.FindGameObjectWithTag("Canvas").GetComponent<MainMenu>().fadeBackToMainMenu();
	}
}