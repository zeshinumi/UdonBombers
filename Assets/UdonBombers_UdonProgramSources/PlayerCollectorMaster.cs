
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class PlayerCollectorMaster : UdonSharpBehaviour
{
	public Material redPedMat;
	public Material greenPedMat;
	public Material greyPedMat;
	public Animator bombAnim;
	public GameControl theGame;
	public Text timerText;
	[HideInInspector]
	public int[] pedPlayersIDs;
	[HideInInspector]
	public bool isLocked;
	private bool isDoingTimer;
	private bool hasLockedPads;
	private float startTimerTime;
	[HideInInspector]
	public bool hasDoneStart;

  void Start() {
		pedPlayersIDs = new int[12];
		for(int i = 0; i < 12; i++) {
			pedPlayersIDs[i] = 0;
		}
		timerText.text = "Start Game";
		hasDoneStart = true;
	}

	public void StartTimer() {
		Debug.Log("===StartTimer");
		//bool isGameActive = (bool)theGame.GetProgramVariable("syncedIsGameActive");
		Debug.Log("isDoingTimer: " + isDoingTimer.ToString());
		Debug.Log("theGame.syncedIsGameActive: " + theGame.syncedIsGameActive.ToString());
		if(!isDoingTimer && !theGame.syncedIsGameActive) {
			Debug.Log("Timer not started and game not started");
			theGame.Play_GameGonnaStart();
			isDoingTimer = true;
			hasLockedPads = false;
			startTimerTime = Time.time;
			bombAnim.Play("BombPulsate");
		}
	}

	public void LockPads() {
		isLocked = true;
	}

	public void UnLockPads() {
		isLocked = false;
		hasLockedPads = false;
		for(int i = 0; i < 12; i++) {
			pedPlayersIDs[i] = 0;
		}
	}

	public bool CanITakePed(int ped, int myID) {
		if(isLocked) {
			return false;
		}
		//bool isGameActive = (bool)theGame.GetProgramVariable("syncedIsGameActive");
		if(theGame.syncedIsGameActive || pedPlayersIDs[ped] != 0) {
			return false;
		}
		foreach(int pedPlayerID in pedPlayersIDs) {
			if(pedPlayerID == myID) {
				return false;
			}
		}
		return true;
	}

	public VRCPlayerApi[] GetPlayers() {
		VRCPlayerApi[] players = new VRCPlayerApi[12];
		int i = 0;
		foreach(int pedPlayerIDs in pedPlayersIDs) {
			if(pedPlayerIDs != 0) {
				VRCPlayerApi newplayer = VRCPlayerApi.GetPlayerById(pedPlayerIDs);
				if(newplayer != null) {
					players[i] = newplayer;
					i++;
				}
			}
		}
		VRCPlayerApi[] finalPlayerList = new VRCPlayerApi[i];
		for(int j = 0; j < i; j++) {
			finalPlayerList[j] = players[j];
		}
		return finalPlayerList;
	}

	private void Update() {
		if(isDoingTimer) {
			if(!hasLockedPads && Time.time - startTimerTime > 8 && Networking.IsOwner(gameObject)) {
				SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LockPads");
				hasLockedPads = true;
			}
			if(Time.time - startTimerTime > 10) {
				if(Networking.IsOwner(gameObject)) {
					theGame.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetUpGame");					
				}
				isDoingTimer = false;
			} else {
				timerText.text = ((int)(10 - (Time.time - startTimerTime))).ToString();
			}
		} else {
			//bool isGameActive = (bool)theGame.GetProgramVariable("syncedIsGameActive");
			if(theGame.syncedIsGameActive) {
				timerText.text = "Game in Progress";
			} else {
				timerText.text = "Start Game";
			}
			if(hasLockedPads && Time.time - startTimerTime > 15 && Networking.IsOwner(gameObject)) {
				SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UnLockPads");
			}
		}
	}
}
