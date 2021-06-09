
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class PlayerList : UdonSharpBehaviour {
	public JoinGameButton joinButton;
	public GameControl theGame;
	public Animator bombAnim;
	public Text timerText;
	[HideInInspector]
	public int numPlayers;
	[HideInInspector]
	public PlayerCaller[] playerCallers;
	[HideInInspector]
	public int myNum;
	[HideInInspector]
	public bool finishedStart, hasLockedPads;
	private float doUpdateObjectStatesTime;
	private bool doUpdateObjectStates, isDoingTimer;
	private float startTimerTime;

	public void InitializePlayerList() {
		playerCallers = new PlayerCaller[transform.childCount];
		for(int i = 0; i < playerCallers.Length; i++) {
			playerCallers[i] = transform.GetChild(i).GetComponent<PlayerCaller>();
			playerCallers[i].SetNumber(i);
		}
		finishedStart = true;
		doUpdateObjectStatesTime = Time.time;
		doUpdateObjectStates = true;
	}

	private void Update() {
		if(doUpdateObjectStates && Time.time - doUpdateObjectStatesTime >= 1f) {
			doUpdateObjectStates = false;
			SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "GetUpdatedState");
		}
		if(isDoingTimer) {
			if(!hasLockedPads && Time.time - startTimerTime > 8 && Networking.IsOwner(gameObject)) {
				joinButton.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LockPads");
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
			if(theGame.syncedIsGameActive) {
				timerText.text = "Game in Progress";
			} else {
				timerText.text = "Start Game";
			}
			if(hasLockedPads && Time.time - startTimerTime > 15 && Networking.IsOwner(gameObject)) {
				joinButton.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UnLockPads");
				hasLockedPads = false;
			}
		}
	}

	public void GetUpdatedState() {
		if(doUpdateObjectStates || !finishedStart) {
			return;
		}
		if(playerCallers[myNum].amInGame) {
			playerCallers[myNum].SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "IAmInTheGame");
		} else {
			playerCallers[myNum].SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "IAmNotInTheGame");
		}
	}

	public void AddMeToGame() {
		if(GetNumInGame() < 12) {
			playerCallers[myNum].SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "IAmInTheGame");
		}
	}
	public void RemoveMeFromGame() {
		playerCallers[myNum].SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "IAmNotInTheGame");
	}

	public void RemoveAllFromGame() {
		foreach(PlayerCaller player in playerCallers) {
			if(player.player == null) {
				break;
			}
			player.amInGame = false;
		}
	}
	
	public int GetNumInGame() {
		int num = 0;
		foreach(PlayerCaller player in playerCallers) {
			if(player.player == null) {
				break;
			}
			if(player.amInGame)
				num++;
		}
		return num;
	}
	public VRCPlayerApi[] GetInGamePlayerList() {
		VRCPlayerApi[] playersInGame = new VRCPlayerApi[0];
		int numAdded = 0;
		foreach(PlayerCaller player in playerCallers) {
			if(player.amInGame) {
				VRCPlayerApi[] newPlayersInGame = new VRCPlayerApi[playersInGame.Length + 1];
				for(int i = 0; i < playersInGame.Length; i++) {
					newPlayersInGame[i] = playersInGame[i];
				}
				newPlayersInGame[playersInGame.Length] = player.player;
				playersInGame = newPlayersInGame;
				numAdded++;
				if(numAdded == 12)
					break;
			}			
		}
		return playersInGame;
	}

	public PlayerCaller GetMyPC() {
		return GetPCByNum(myNum);
	}
	public PlayerCaller GetPCByPlayer(VRCPlayerApi player) {
		return GetPCByNum(PlayerToArrayNum(player));
	}
	public PlayerCaller GetPCByNum(int num) {
		if(num == -1 || num >= playerCallers.Length) {
			return null;
		} else {
			return playerCallers[num];
		}
	}

	public override void OnPlayerJoined(VRCPlayerApi player) {
		if(!finishedStart) {
			InitializePlayerList();
		}
		AddPlayer(player);
	}
	public override void OnPlayerLeft(VRCPlayerApi player) {
		if(!finishedStart) {
			InitializePlayerList();
		}
		RemovePlayer(player);
		joinButton.UpdatePeds();
	}

	private void SwapPlayers(PlayerCaller p1, PlayerCaller p2) {
		VRCPlayerApi tempPlayer = p1.player;
		bool tempAmInGame = p1.amInGame;
		p1.player = p2.player;
		p1.amInGame = p2.amInGame;
		p2.player = tempPlayer;
		p2.amInGame = tempAmInGame;
	}

	private void AddPlayer(VRCPlayerApi newPlayer) {
		playerCallers[numPlayers].SetUpCaller(numPlayers, newPlayer);
		numPlayers++;
		SortPlayerList();
		myNum = PlayerToArrayNum(Networking.LocalPlayer);
	}
	private void RemovePlayer(VRCPlayerApi removedPlayer) {
		if(playerCallers[numPlayers - 1].player == removedPlayer) {
			numPlayers--;
			playerCallers[numPlayers].player = null;
			playerCallers[numPlayers].amInGame = false;
		} else {
			bool foundPlayer = false;
			for(int i = 0; i < numPlayers - 1; i++) {
				if(foundPlayer) {
					SwapPlayers(playerCallers[i], playerCallers[i + 1]);
				} else if(removedPlayer == playerCallers[i].player) {
					foundPlayer = true;
					playerCallers[i].player = null;
					playerCallers[i].amInGame = false;
				}
			}
			if(foundPlayer) {
				SortPlayerList();
				numPlayers--;
				myNum = PlayerToArrayNum(Networking.LocalPlayer);
			}
		}
	}

	private void SortPlayerList() {
		for(int i = 0; i < numPlayers; i++) {
			for(int j = i; j < numPlayers; j++) {
				if(playerCallers[j].player == null)
					continue;
				if(playerCallers[i].player == null || playerCallers[j].player.playerId < playerCallers[i].player.playerId ||
					(playerCallers[j].player.playerId == playerCallers[i].player.playerId && playerCallers[j].player.displayName.CompareTo(playerCallers[i].player.displayName) < 0)) {

					SwapPlayers(playerCallers[i], playerCallers[j]);
				}
			}
		}
	}

	public int PlayerToArrayNum(VRCPlayerApi player) {
		for(int i = 0; i < numPlayers; i++) {
			if(playerCallers[i].player == player) {
				return i;
			}
		}
		return -1;
	}

	public void StartTimer() {
		if(!isDoingTimer && !theGame.syncedIsGameActive) {
			theGame.Play_GameGonnaStart();
			isDoingTimer = true;
			hasLockedPads = false;
			startTimerTime = Time.time;
			bombAnim.Play("BombPulsate");
		}
	}

}
