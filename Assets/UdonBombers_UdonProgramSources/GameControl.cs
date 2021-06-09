
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;
using UnityEngine.UI;

public class GameControl : UdonSharpBehaviour {
	private string queue = "";
	private string lastQueue = "";
	private float lastTime;
	private const int payloadDelay = 1;
	private const int maxPayloadLength = 100;
	private UnityEngine.Random classRand;

	private const int DETECTABLE_OBJECTS_LAYER = 24;
	private const int LOCAL_PLAYER_ONLY_LAYER = 23;
	private int detectable_objects_layerMask = (1 << DETECTABLE_OBJECTS_LAYER) | (1 << LOCAL_PLAYER_ONLY_LAYER);

	private const int POWERUP_PIERCING = 1;
	private const int POWERUP_MAXBOMB = 2;
	private const int POWERUP_BLASTRADIUS = 3;
	private const int POWERUP_EXTRABOMBS = 4;
	private const int POWERUP_SPEED = 5;

	private Vector3 minimapSpot = new Vector3(10.38f, -5.86f, -76);

	[UdonSynced]
	private int seed;
	
	public Text winnerNameOutput;
	public Text winnerNameShadowOutput;
	public Animator winningAnim;

	public AudioSource[] gameAboutToStart_audio;
	public AudioSource[] readyStart_audio;
	public AudioSource[] hurryUp_audio;
	public AudioSource[] finish_audio;

	/// Outputs ///
	public Text assignedPlayerOutput;
	public Text isActiveOutput;
	public Text hasStartedOutput;
	public Text isGameActiveOutput;
	public Text isDeadOutput;
	public Text startPosOutput;
	public Text magFromStartOutput;
	public Text thisPlayerIDOutput;
	public Text localPlayerIDOutput;
	public Text isLoadingPlayersOutput;
	public Text hasClearedArenaOutput;
	///////////////

	private bool doEndGame;
	private float startTimerForEndGame;

	public PlayerList playersCollecter;
	private Players[] players;
	[HideInInspector]
	public Players assignedPlayer;
	public GameObject[] collidableObjectsThatGetInTheWay;
	public GameObject spawnSpotsParent;
	public GameObject deathSpawnSpotsParent;
	public GameObject brick;
	public Transform respawnLoc;
	public GameObject damageObj;
	public GameObject[] powerUps;
	public AudioSource powerupSound;
	public AudioSource explosionSound;
	public AudioSource bombPlacedSound;
	public AudioSource bgMusic_lobby;
	public AudioSource bgMusic_game;
	private Transform[] spawnSpots;
	public Transform mainSpawn;
	private Transform[] deathSpawns;
	private Vector3[] spiraledPositions;
	private string dontTouchXZ;
	[HideInInspector]
	public string allBricks;
	public string allWalls;

	private string allBombs = "";
	private string bombsToRemove = "";
	private string extraBombs = "";
	public GameObject explode;
	public GameObject explode_blue;
	public GameObject bomb;
	public GameObject bigBomb;
	public GameObject pierceBomb;
	public GameObject skyBomb;
	public GameObject goldBomb;
	public GameObject goldBigBomb;
	public GameObject goldPierceBomb;
	public GameObject goldSkyBomb;
	private string alreadyExploded = "";

	private float waitTimeForLoadingPlayers;
	private bool isLoadingPlayers;
	private bool hasTeleportedPlayers;
	[HideInInspector]
	public bool isGameActive;
	[UdonSynced][HideInInspector]
	public bool syncedIsGameActive;

	private int numPlayers;
	private string powerUpSpaces;

	public GameObject deathSpike;
	private float gameStartTime;
	private float lastSpikeDropedTime;
	private bool startDropingSpikes;
	private int curSpikeDropLoc;
	private int numPlayersAlive;
	private bool hasClearedArena;

	//Players Info
	private int myGameID;
	private string[] patreonList = {"zeshin", "leon conner", "cheddarpopcorn", "ashfirwind", "majorblue7"};

	private void Start() {
		Networking.LocalPlayer.CombatSetup();
		Networking.LocalPlayer.CombatSetDamageGraphic(damageObj);
		Networking.LocalPlayer.CombatSetRespawn(true, 5, respawnLoc);
		Networking.LocalPlayer.CombatSetMaxHitpoints(1);
		Networking.LocalPlayer.CombatSetCurrentHitpoints(1);
		hasClearedArena = true;
		ConstructSpiral();
		lastTime = Time.time;
		players = GetComponentsInChildren<Players>();
		foreach(Players player in players) {
			player.Initialize();
		}
		spawnSpots = new Transform[spawnSpotsParent.transform.childCount];
		for(int i = 0; i < spawnSpots.Length; i++) {
			spawnSpots[i] = spawnSpotsParent.transform.GetChild(i);
			AppendToPlayerPlus((int)spawnSpots[i].position.x, (int)spawnSpots[i].position.z);
		}
		deathSpawns = new Transform[deathSpawnSpotsParent.transform.childCount];
		for(int i = 0; i < deathSpawns.Length; i++) {
			deathSpawns[i] = deathSpawnSpotsParent.transform.GetChild(i);
			AppendToPlayerPlus((int)deathSpawns[i].position.x, (int)deathSpawns[i].position.z);
		}

		if(Networking.IsMaster) {
			seed = DateTime.Now.Millisecond;
		}
		allWalls = "";
		for(int x = 2; x<=38; x+=4) {
			for(int z = 2; z <= 38; z+=4) {
				allWalls = AddToQueue(GetConvertedVectorByInt(x, z), allWalls);
			}
		}
		bgMusic_lobby.Play();

		powerUpSpaces = "";
	}

	public bool IsPatron(string userName) {
		foreach(string name in patreonList) {
			if(name.ToLower() == userName.ToLower()) {
				return true;
			}
		}
		return false;
	}

	private void ConstructSpiral() {
		spiraledPositions = new Vector3[441];
		int dirUp = 1;
		int dirRight = 0;
		Vector3 curPos = new Vector3(0, 1, 0);
		string posBeenAt = "";
		for(int i = 0; i < spiraledPositions.Length; i++) {
			spiraledPositions[i] = curPos;
			posBeenAt = AddToQueue(GetConvertedVector(curPos), posBeenAt);
			if(dirUp == 1) {
				if(posBeenAt.Contains(GetConvertedVectorByFloat(curPos.x, curPos.z + 2)) || curPos.z+2 > 40){
					dirUp = 0;
					dirRight = 1;
					curPos = new Vector3(curPos.x + 2, curPos.y, curPos.z);
				} else {
					curPos = new Vector3(curPos.x, curPos.y, curPos.z+2);
				}
			}else if(dirRight == 1) {
				if(posBeenAt.Contains(GetConvertedVectorByFloat(curPos.x+2, curPos.z)) || curPos.x+2 > 40) {
					dirUp = -1;
					dirRight = 0;
					curPos = new Vector3(curPos.x, curPos.y, curPos.z - 2);
				} else {
					curPos = new Vector3(curPos.x+2, curPos.y, curPos.z);
				}
			} else if(dirUp == -1) {
				if(posBeenAt.Contains(GetConvertedVectorByFloat(curPos.x, curPos.z-2)) || curPos.z-2 < 0) {
					dirUp = 0;
					dirRight = -1;
					curPos = new Vector3(curPos.x - 2, curPos.y, curPos.z);
				} else {
					curPos = new Vector3(curPos.x, curPos.y, curPos.z-2);
				}
			} else if(dirRight == -1) {
				if(posBeenAt.Contains(GetConvertedVectorByFloat(curPos.x-2, curPos.z)) || curPos.x-2 < 0) {
					dirUp = 1;
					dirRight = 0;
					curPos = new Vector3(curPos.x, curPos.y, curPos.z + 2);
				} else {
					curPos = new Vector3(curPos.x-2, curPos.y, curPos.z);
				}
			}
		}
	}

	private void AppendToPlayerPlus(int x, int z) {
		dontTouchXZ = AddToQueue(ConvertId(x) + ConvertId(z), dontTouchXZ);
		dontTouchXZ = AddToQueue(ConvertId(x + 2) + ConvertId(z), dontTouchXZ);
		if(x - 2 >= 0) {
			dontTouchXZ = AddToQueue(ConvertId(x - 2) + ConvertId(z), dontTouchXZ);
		}
		dontTouchXZ = AddToQueue(ConvertId(x) + ConvertId(z + 2), dontTouchXZ);
		if(z - 2 >= 0) {
			dontTouchXZ = AddToQueue(ConvertId(x) + ConvertId(z - 2), dontTouchXZ);
		}
	}

	private string ConvertId(int playerId) {
		if(playerId < 10) {
			return "0" + playerId.ToString();
		} else {
			return playerId.ToString();
		}
	}

	public void SetCollidableObjectsInWay(bool setActive) {
		foreach(GameObject objInWay in collidableObjectsThatGetInTheWay) {
			objInWay.SetActive(setActive);
		}
	}

	public void MakeEveryoneSetUpGame() {
		SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetUpGame");
	}

	public void SetUpGame() {
		if(LoadUpPlayers()) {
			doEndGame = false;
			SetStage();
			startDropingSpikes = false;
			curSpikeDropLoc = 0;
			hasClearedArena = false;
		}
	}

	private void ClearStage() {
		Collider[] allObjects = Physics.OverlapBox(new Vector3(21, 0, 21), new Vector3(22, 5, 22), Quaternion.identity, detectable_objects_layerMask);
		foreach(Collider detObj in allObjects) {
			if(detObj != null && !detObj.name.Contains("AreaCube")) {
				Destroy(detObj.gameObject);
			}
		}
		hasClearedArena = true;
		allBricks = "";
		allBombs = "";
		powerUpSpaces = "";
	}

	private void SetUpimerForEndGame() {
		if(!doEndGame) {
			doEndGame = true;
			startTimerForEndGame = Time.time;
		}
	}
	public void StopEndGameTimer() {
		doEndGame = false;
		startTimerForEndGame = Time.time;
	}

	public void EndGame() {
		Play_Finish();
		doEndGame = false;
		isGameActive = false;
		startDropingSpikes = false;
		ClearStage();
		if(assignedPlayer != null && assignedPlayer.thisPlayer != null && assignedPlayer.isActive && Networking.LocalPlayer.playerId == assignedPlayer.thisPlayer.playerId) {
			SetCollidableObjectsInWay(false);
			Networking.LocalPlayer.SetRunSpeed(4);
			Networking.LocalPlayer.SetWalkSpeed(2);
			Networking.LocalPlayer.TeleportTo(mainSpawn.position, mainSpawn.rotation);
		}
		string winnerName = "";
		foreach(Players player in players) {
			if(!player.isDead && player.thisPlayer != null) {
				winnerName = player.thisPlayer.displayName;
			}
			player.isActive = false;
			player.isDead = true;
		}
		if(winnerName == "") {
			winningAnim.Play("draw");
		} else {
			winnerNameOutput.text = winnerName;
			winnerNameShadowOutput.text = winnerName;
			winningAnim.Play("win");
		}
		assignedPlayer = null;
		if(Networking.IsOwner(gameObject)) {
			syncedIsGameActive = false;
			seed = DateTime.Now.Millisecond;
		}
		bgMusic_game.Stop();
		bgMusic_lobby.Play();
	}

	private bool LoadUpPlayers() {
		VRCPlayerApi[] newPlayers = playersCollecter.GetInGamePlayerList();
		if(newPlayers.Length == 0 || newPlayers[0] == null) {
			return false;
		}
		numPlayers = 0;
		for(int i = 0; i < newPlayers.Length; i++) {
			if(newPlayers[i] == null || VRCPlayerApi.GetPlayerById(newPlayers[i].playerId) == null) {
				continue;
			}
			players[numPlayers].SetPlayer(newPlayers[i]);
			players[numPlayers].startPos = spawnSpots[numPlayers];
			if(Networking.LocalPlayer.playerId == newPlayers[i].playerId) {
				assignedPlayer = players[numPlayers];
			}
			numPlayers++;
		}
		isLoadingPlayers = true;
		hasTeleportedPlayers = false;
		waitTimeForLoadingPlayers = Time.time;
		return true;
	}

	private void TeleportToArena() {
		gameStartTime = Time.time;
		if(assignedPlayer != null && assignedPlayer.thisPlayer != null && assignedPlayer.startPos != null) {
			SetCollidableObjectsInWay(false);
			bgMusic_lobby.Stop();
			bgMusic_game.Play();
			bgMusic_game.pitch = 1.0f;
			assignedPlayer.thisPlayer.TeleportTo(assignedPlayer.startPos.position, assignedPlayer.startPos.rotation);
		}
	}

	public Vector3 ConvertPosToGrid(Vector3 targetPos) {
		int newX;
		if((int)targetPos.x % 2 != 0) {
			newX = (int)targetPos.x + 1;
		} else {
			newX = (int)targetPos.x;
		}
		if(newX < 0) {
			newX = 0;
		}

		int newZ = (int)targetPos.z;
		if((int)targetPos.z % 2 != 0) {
			newZ = (int)targetPos.z + 1;
		} else {
			newZ = (int)targetPos.z;
		}
		if(newZ < 0) {
			newZ = 0;
		}
		return new Vector3(newX, 1, newZ);
	}

	public void SetStage() {
		ClearStage();
		UnityEngine.Random.InitState(seed);
		for(int x = 0; x < 42; x+=2) {
			for(int z = 0; z < 42; z+=2) {
				if(dontTouchXZ.Contains(ConvertId(x) + ConvertId(z))) {
					continue;
				}
				if(UnityEngine.Random.Range(0, 3) <= 1) {
					if(!IsWallAt(x, z)) {
						SpawnObject(brick, new Vector3(x, 1, z));
						allBricks = AddToQueue(ConvertId(x) + ConvertId(z) + "+" + ReturnRandomPowerup().ToString(), allBricks);
					}
				}
			}
		}
	}

	public string GetConvertedVector(Vector3 pos) {
		return ConvertId((int)pos.x) + ConvertId((int)pos.z);
	}
	public string GetConvertedVectorByInt(int x, int z) {
		return ConvertId(x) + ConvertId(z);
	}
	public string GetConvertedVectorByFloat(float x, float z) {
		return ConvertId((int)x) + ConvertId((int)z);
	}

	public bool IsBrickAt(float x, float z) {
		return allBricks.Contains(GetConvertedVectorByFloat(x,z));
	}
	public bool IsWallAt(float x, float z) {
		return allWalls.Contains(GetConvertedVectorByFloat(x, z));
	}
	public void RemoveBrick(string brick) {
		allBricks = RemoveFromQueue(brick, allBricks);
	}

	public void SpawnObject(GameObject spawnObject, Vector3 spawnPos) {
		GameObject newSpawn = VRCInstantiate(spawnObject);
		newSpawn.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
		newSpawn.SetActive(true);
	}

	private void Update() {
		if(isGameActive) {
			if(startDropingSpikes) {
				if(Time.time - lastSpikeDropedTime > 0.15f) {
					startDropingSpikes = DropNextSpike();
					if(!startDropingSpikes) {
						SetUpimerForEndGame();
					}
					lastSpikeDropedTime = Time.time;
				}
			} else {
				if(Time.time - gameStartTime > 240.0f) {
					Play_HurryUp();
					bgMusic_game.pitch = 1.1f;
					startDropingSpikes = true;
					lastSpikeDropedTime = Time.time;
				}
			}
		}else if(isLoadingPlayers) {
			if(Time.time - waitTimeForLoadingPlayers > 1) {
				if(!hasTeleportedPlayers) {
					TeleportToArena();
					hasTeleportedPlayers = true;
				}
				if(AllPlayersInArena()) {
					isLoadingPlayers = false;
					isGameActive = true;
					if(Networking.IsOwner(gameObject)) {
						syncedIsGameActive = true;
					}
					Play_ReadyStart();
				}
			}
		}	else if(!hasClearedArena) {
			SetUpimerForEndGame();
		} 
		if((Networking.LocalPlayer.GetPosition() - respawnLoc.position).magnitude <= 12.0f) {
			SetCollidableObjectsInWay(true);
		}
		if(assignedPlayer==null && IsLocalPlayerInArena()) {
			Networking.LocalPlayer.TeleportTo(respawnLoc.position, respawnLoc.rotation);
		}
	}

	private bool AllPlayersInArena() {
		foreach(Players player in players) {
			if(player.isActive && player.thisPlayer != null  && !player.IsPlayerInArena()) {
				return false;
			}
		}
		return true;
	}

	private bool DropNextSpike() {
		if(curSpikeDropLoc > spiraledPositions.Length) {
			return false;
		}
		if(curSpikeDropLoc != 0) {
			Vector3 lastSpikePos = spiraledPositions[curSpikeDropLoc];
			if(assignedPlayer!=null && !assignedPlayer.isDead && 
				assignedPlayer.GetConvertedPos().x == lastSpikePos.x && assignedPlayer.GetConvertedPos().z == lastSpikePos.z) {
				assignedPlayer.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "KillPlayer");
			}
		}
		GameObject newSpike = VRCInstantiate(deathSpike);
		newSpike.transform.SetPositionAndRotation(spiraledPositions[curSpikeDropLoc], Quaternion.identity);
		newSpike.SetActive(true);
		curSpikeDropLoc++;
		return true;
	}

	private void LateUpdate() {
		///////////// OUTPUTS /////////////////////////////////
		/*if(assignedPlayer == null) {
			assignedPlayerOutput.text = "No assigned Player";
			isActiveOutput.text = "No assigned Player";
			hasStartedOutput.text = "No assigned Player";
			isDeadOutput.text = "No assigned Player";
			startPosOutput.text = "No assigned Player";
			thisPlayerIDOutput.text = "No assigned Player";

		} else {
			assignedPlayerOutput.text = assignedPlayer.name;
			isActiveOutput.text = assignedPlayer.isActive.ToString();
			hasStartedOutput.text = assignedPlayer.hasStartedGame.ToString();
			isDeadOutput.text = assignedPlayer.isDead.ToString();
			if(assignedPlayer.startPos == null) {
				startPosOutput.text = "No starting position";
			} else {
				startPosOutput.text = assignedPlayer.startPos.position.ToString();
			}
			if(assignedPlayer.thisPlayer == null) {
				thisPlayerIDOutput.text = "No player on Assigned Player";
			} else {
				thisPlayerIDOutput.text = assignedPlayer.thisPlayer.playerId.ToString();
			}
		}
		isGameActiveOutput.text = isGameActive.ToString();
		magFromStartOutput.text = (Networking.LocalPlayer.GetPosition()-respawnLoc.position).magnitude.ToString();
		localPlayerIDOutput.text = Networking.LocalPlayer.playerId.ToString();
		isLoadingPlayersOutput.text = isLoadingPlayers.ToString();
		hasClearedArenaOutput.text = hasClearedArena.ToString();*/
		///////////// OUTPUTS /////////////////////////////////
		if(isGameActive) {
			if(allBombs.Length > 0) {
				string[] allBombsExposed = allBombs.Split(':');
				int i = 0;
				foreach(string pBomb in allBombsExposed) {
					string[] bombParts = pBomb.Split('+');
					if(Time.time - float.Parse(bombParts[3]) > 4.0f) {
						explosionSound.Play();
						StartExplosionChain(i);
						break;
					}
					i++;
				}
			}
			numPlayersAlive = 0;
			foreach(Players player in players) {
				if(player.ShouldBeDead()) {
					player.isDead = true;
				}
				if(player.isActive && !player.isDead) {
					numPlayersAlive++;
					Vector3 convVector = player.GetConvertedPos();
					if(player.hasStartedGame && allBricks.Contains(GetConvertedVector(convVector))) {
						DestroyBrickAtPos(convVector);
					}
					string powerUpDetected = GetPowerUpNextToPos(convVector);
					if(powerUpDetected != "") {
						PickUpPowerUp(powerUpDetected, player);
					}					
				}
			}
			if(numPlayersAlive <= 1) {
				SetUpimerForEndGame();
			}
			if(doEndGame && Time.time - startTimerForEndGame > 3f) {
				EndGame();
			}
		}
		if(assignedPlayer == null) {
			float mag = (Networking.LocalPlayer.GetPosition() - minimapSpot).magnitude;
			if(mag > 15) mag = 15;
			explosionSound.volume = 0.8f * ((15 - mag) / 15);
		} else {
			explosionSound.volume = 0.8f;
		}
	}

	public string AddToQueue(string newMessage, string curStr) {
		if(curStr == "") {
			curStr += newMessage;
		} else {
			curStr += ":" + newMessage;
		}
		return curStr;
	}

	public string RemoveFromQueue(string targetMessage, string curStr) {
		if(!curStr.Contains(targetMessage)) {
			return curStr;
		}
		if(curStr.IndexOf(targetMessage) == 0) {
			if(curStr.Contains(":")) {
				curStr = curStr.Replace(targetMessage + ":", "");
			} else {
				curStr = curStr.Replace(targetMessage, "");
			}
		} else {
			if(curStr.Contains(":")) {
				curStr = curStr.Replace(":" + targetMessage, "");
			} else {
				curStr = curStr.Replace(targetMessage, "");
			}
		}
		return curStr;
	}
	
	public void ProcessPayLoad(string payload) {
		string[] wLoad = payload.Split(':');
		foreach(string instruct in wLoad) {
			if(instruct.Contains("SetPID")) {
				string[] items = instruct.Split('+');
				SetPlayerID(int.Parse(items[1]), int.Parse(items[2]));
			}
		}
	}

	private void SetPlayerID(int targetPlayerId, int playerNum) {
		if(targetPlayerId == Networking.LocalPlayer.playerId) {
			myGameID = playerNum;
		}
	}

	private void RemoveBombs() {
		string[] removeBombs = bombsToRemove.Split(':');
		foreach(string bombs in removeBombs) {
			string[] bombParts = bombs.Split('+');
			Vector3 bombPos = VectorFromConvertedID(bombParts[0]);
			Collider[] colObjs = Physics.OverlapSphere(new Vector3(bombPos.x, 1, bombPos.z), 0.5f, detectable_objects_layerMask);
			if(colObjs != null) {
				foreach(Collider colObj in colObjs) {
					if(colObj.name.Contains(bomb.name)) {
						Destroy(colObj.gameObject);
						break;
					}
				}
			}
			allBombs = RemoveFromQueue(bombs, allBombs);
		}
	}

	public bool IsOnBomb(Vector3 pos) {
		return allBombs.Contains(GetConvertedVector(pos));
	}

	public Players GetPlayerByPlayerID(int playerID) {
		foreach(Players p in players) {
			if(p.thisPlayerID == playerID) {
				return p;
			}
		}
		return null;
	}

	public void CreateBomb(Vector3 bombPos, bool isPenetrating, int blastRadius, bool playerIsDead, Vector3 playerTruePos, bool isPatron, int playerID) {
		GameObject newBombObj;
		if(playerIsDead) {
			newBombObj = VRCInstantiate(isPatron ? goldSkyBomb : skyBomb);
			if(playerTruePos.x <= -1) {
				for(int i = 0; i <= 40; i+=2) {
					string convPos = GetConvertedVectorByFloat(bombPos.x + i, bombPos.z);
					if(!(allBricks.Contains(convPos) || allWalls.Contains(convPos) || allBombs.Contains(convPos))) {
						bombPos = new Vector3(bombPos.x + i, bombPos.y, bombPos.z);
						break;
					}
				}
			}else if(playerTruePos.z >= 41) {
				for(int i = 0; i <= 40; i+=2) {
					string convPos = GetConvertedVectorByFloat(bombPos.x, bombPos.z - i);
					if(!(allBricks.Contains(convPos) || allWalls.Contains(convPos) || allBombs.Contains(convPos))) {
						bombPos = new Vector3(bombPos.x, bombPos.y, bombPos.z - i);
						break;
					}
				}
			} else if(playerTruePos.x >= 41) {
				for(int i = 0; i <= 40; i+=2) {
					string convPos = GetConvertedVectorByFloat(bombPos.x - i, bombPos.z);
					if(!(allBricks.Contains(convPos) || allWalls.Contains(convPos) || allBombs.Contains(convPos))) {
						bombPos = new Vector3(bombPos.x - i, bombPos.y, bombPos.z);
						break;
					}
				}
			} else {
				for(int i = 0; i <= 40; i+=2) {
					string convPos = GetConvertedVectorByFloat(bombPos.x, bombPos.z + i);
					if(!(allBricks.Contains(convPos) || allWalls.Contains(convPos) || allBombs.Contains(convPos))) {
						bombPos = new Vector3(bombPos.x, bombPos.y, bombPos.z + i);
						break;
					}
				}
			}
			string powerup = GetPowerUpNextToPos(bombPos);
			if(powerup!="") {
				RemovePowerUp(powerup);
			}
		} else {
			if(blastRadius == 21) {
				newBombObj = VRCInstantiate(isPatron ? goldBigBomb : bigBomb);
			} else if(isPenetrating) {
				newBombObj = VRCInstantiate(isPatron ? goldPierceBomb : pierceBomb);
			} else {
				newBombObj = VRCInstantiate(isPatron ? goldBomb : bomb);
			}
		}
		newBombObj.transform.SetPositionAndRotation(bombPos, Quaternion.identity);
		newBombObj.SetActive(true);
		float mag = (Networking.LocalPlayer.GetPosition() - bombPos).magnitude;
		if(mag > 10)
			mag = 10;
		bombPlacedSound.volume = 0.6f * ((10 - mag) / 10);
		bombPlacedSound.Play();
		string newBomb = CreateBombString(bombPos, isPenetrating, blastRadius, playerID);
		allBombs = AddToQueue(newBomb, allBombs);
	}
	
	private int FindBombIndexByPos(Vector3 findPos) {
		if(allBombs.Contains(GetConvertedVector(findPos))) {
			string[] allBombsExposed = allBombs.Split(':');
			int i = 0;
			foreach(string pBomb in allBombsExposed) {
				if(pBomb.Contains(GetConvertedVector(findPos))) {
					return i;
				}
				i++;
			}
		}
		return -1;
	}

	private int GetBombIndex(string targetBomb) {
		string[] daBombs = allBombs.Split(':');
		int index = 0;
		foreach(string bomb in daBombs) {
			if(bomb == targetBomb) {
				return index;
			}
			index++;
		}
		return -1;
	}

	private Players[] FindPlayerByPos(Vector3 findPos) {
		Players[] foundPlayers = null;
		int foundCount = 0;
		foreach(Players player in players) {
			if(!player.isDead && player.GetConvertedPos().x==findPos.x && player.GetConvertedPos().z == findPos.z) {
				foundCount++;
				Players[] tempP = new Players[foundCount];
				if(foundPlayers == null) {
					tempP[0] = player;
				} else {
					for(int i = 0; i < foundPlayers.Length; i++) {
						tempP[i] = foundPlayers[i];
					}
					tempP[foundPlayers.Length] = player;
				}
				foundPlayers = tempP;
			}
		}
		return foundPlayers;
	}
	private GameObject FindBrickByPos(Vector3 findPos) {
		GameObject foundBrick = null;
		if(allBricks.Contains(GetConvertedVector(findPos))){
			Collider[] colObjs = Physics.OverlapSphere(new Vector3(findPos.x, 2, findPos.z), 0.5f, detectable_objects_layerMask);
			if(colObjs != null && colObjs.Length >= 1) {
				foreach(Collider colObj in colObjs) {
					if(colObj.name.Contains(brick.name)) {
						foundBrick = colObj.gameObject;
						break;
					}
				}
			}
		}
		return foundBrick;		
	}

	public void SpawnExplosion(Vector3 atPos, bool isBombCenter, Players playerOwner) {
		if(alreadyExploded.Contains(GetConvertedVector(atPos)) || IsWallAt(atPos.x, atPos.z)) {
			return;
		}
		alreadyExploded = AddToQueue(GetConvertedVector(atPos), alreadyExploded);
		GameObject newExplode = VRCInstantiate(playerOwner.isPatron ? explode_blue : explode);
		newExplode.transform.SetPositionAndRotation(atPos, Quaternion.identity);
		newExplode.SetActive(true);
		string powerup = GetPowerUpNextToPos(atPos);
		if(powerup != "") {
			RemovePowerUp(powerup);
		}
		if(!isBombCenter) {
			int bombAtExplosion = FindBombIndexByPos(atPos);
			if(bombAtExplosion != -1) {
				extraBombs = AddToQueue(allBombs.Split(':')[bombAtExplosion], extraBombs);
			}
			if(allBricks.Contains(GetConvertedVector(atPos))) {
				DestroyBrickAtPos(atPos);
			}
		}

		if(assignedPlayer != null && !assignedPlayer.isDead && assignedPlayer.GetConvertedPos().x == atPos.x && assignedPlayer.GetConvertedPos().z == atPos.z) {
			if(playerOwner != null && playerOwner.isDead && playerOwner.thisPlayer != null) {
				playerOwner.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RevivePlayer");
				playerOwner.isDead = false;
			}
			assignedPlayer.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "KillPlayer");
		}		
	}

	private void DestroyBrickAtPos(Vector3 atPos) {
		GameObject foundBrick = FindBrickByPos(atPos);
		Destroy(foundBrick);
		string brick = allBricks.Substring(allBricks.IndexOf(ConvertId((int)atPos.x) + ConvertId((int)atPos.z)), 6);
		SpawnPowerUp(int.Parse(brick.Split('+')[1]), new Vector3(atPos.x, 1, atPos.z));
		RemoveBrick(brick);
	}

	private string CreateBombString(Vector3 bombPos, bool isPenetrating, int blastRadius, int playerID) {
		return GetConvertedVector(bombPos) + "+" + (isPenetrating ? "1" : "0") + "+" + blastRadius.ToString() + "+" + Time.time.ToString() + "+"  + playerID.ToString();
	}
	private Vector3 VectorFromConvertedID(string id) {
		return new Vector3(int.Parse(id.Substring(0, 2)), 1, int.Parse(id.Substring(2, 2)));
	}

	private Vector3 GetRandomValidPos() {
		for(int i = 0; i < 500; i++) {
			Vector3 testPos = ConvertPosToGrid(new Vector3(UnityEngine.Random.Range(0, 41), 1, UnityEngine.Random.Range(0, 41)));
			string convertPos = GetConvertedVector(testPos);
			if(!allBombs.Contains(convertPos) && !allWalls.Contains(convertPos) && !powerUpSpaces.Contains(convertPos) && !allBricks.Contains(convertPos) ) {
				return testPos;
			}
		}
		return new Vector3(0, 1, 0);
	}

	private bool IsLocalPlayerInArena() {
		Vector3 playerPos = Networking.LocalPlayer.GetPosition();
		return playerPos.x >= -3.0f && playerPos.x <= 44.0f && playerPos.z >= -3.0f && playerPos.z <= 44.0f;
	}

	private int HowManyLivingPlayers() {
		int alivePlayers = 0;
		foreach(Players player in players) {
			if(player!=null && !player.isDead && player.isActive) {
				alivePlayers++;
			}
		}
		return alivePlayers;
	}

	private Transform GetRespawnLoc(Vector3 curPos) {
		if(HowManyLivingPlayers() <= 1 || !isGameActive) {
			return respawnLoc;
		}
		Transform newSpawn = deathSpawns[0];
		foreach(Transform ds in deathSpawns) {
			if((curPos-ds.position).magnitude < (newSpawn.position - curPos).magnitude) {
				newSpawn = ds;
			}
		}
		return newSpawn;
	}

	public void KillPlayer(Players playerToKill) {
		if(playerToKill.thisPlayer == null) {
			return;
		}
		playerToKill.isDead = true;
		if(playerToKill.thisPlayer == Networking.LocalPlayer) {
			Transform newSpawnLoc = GetRespawnLoc(Networking.LocalPlayer.GetPosition());
			Networking.LocalPlayer.CombatSetRespawn(true, 5, newSpawnLoc);
			Networking.LocalPlayer.CombatSetCurrentHitpoints(-1);
		}
		int newSeed = (playerToKill.thisPlayer.playerId * int.Parse(GetConvertedVector(playerToKill.GetConvertedPos()))) + seed;
		UnityEngine.Random.InitState(newSeed);
		for(int i = 1; i < playerToKill.blastRadiusPowerup; i++) {
			SpawnPowerUp(POWERUP_BLASTRADIUS, GetRandomValidPos());
		}
		for(int i = 1; i < playerToKill.bombCountPowerup; i++) {
			SpawnPowerUp(POWERUP_EXTRABOMBS, GetRandomValidPos());
		}
		for(int i = 1; i < playerToKill.speedPowerup; i++) {
			SpawnPowerUp(POWERUP_SPEED, GetRandomValidPos());
		}
		if(playerToKill.isPenPowerup) {
			SpawnPowerUp(POWERUP_PIERCING, GetRandomValidPos());
		}
		if(playerToKill.isInfiBombPowerup) {
			SpawnPowerUp(POWERUP_MAXBOMB, GetRandomValidPos());
		}
		playerToKill.blastRadiusPowerup = 1;
		playerToKill.bombCountPowerup = 1;
		playerToKill.speedPowerup = 1;
		playerToKill.isInfiBombPowerup = false;
		playerToKill.isPenPowerup = false;
		playerToKill.bombsLeft = 1;
	}

	public void StartExplosionChain(int startNum) {
		alreadyExploded = "";
		bombsToRemove = "";
		extraBombs = allBombs.Split(':')[startNum];
		while(extraBombs != "") {
			string nextBomb = extraBombs.Split(':')[0];
			GoBoom(GetBombIndex(nextBomb));
			extraBombs = RemoveFromQueue(nextBomb, extraBombs);
		}
		RemoveBombs();
	}

	public void GoBoom(int num) {
		string getBomb = allBombs.Split(':')[num];
		bombsToRemove = AddToQueue(getBomb, bombsToRemove);
		string[] theBomb = getBomb.Split('+');
		Vector3 bombPos = VectorFromConvertedID(theBomb[0]);
		bool isPenetrating = theBomb[1] == "1";
		int blastRadius = int.Parse(theBomb[2]);
		Players bombOwner = GetPlayerByPlayerID(int.Parse(theBomb[4]));

		bool stopLeft = false;
		bool stopRight = false;
		bool stopUp = false;
		bool stopDown = false;

		bool wallLeft = false;
		bool wallRight = false;
		bool wallUp = false;
		bool wallDown = false;

		for(int i = 0; i <= blastRadius * 2; i += 2) {
			if(i == 0) {
				SpawnExplosion(bombPos, true, bombOwner);
			} else {
				int posX = (int)bombPos.x;
				int posZ = (int)bombPos.z;
				if(!wallLeft && (!stopLeft || isPenetrating) && posX - i >= 0) {
					wallLeft = wallLeft || IsWallAt(posX - i, posZ) || IsOnBomb(new Vector3(posX - i,0, posZ));
					stopLeft = stopLeft || IsBrickAt(posX - i, posZ);
					SpawnExplosion(new Vector3(posX - i, 1, posZ), false, bombOwner);
				}
				if(!wallRight && (!stopRight || isPenetrating) && posX + i <= 42) {
					wallRight = wallRight || IsWallAt(posX + i, posZ) || IsOnBomb(new Vector3(posX + i,0, posZ));
					stopRight = stopRight || IsBrickAt(posX + i, posZ);
					SpawnExplosion(new Vector3(posX + i, 1, posZ), false, bombOwner);
				}
				if(!wallDown && (!stopDown || isPenetrating && posZ - i >= 0)) {
					wallDown = wallDown || IsWallAt(posX, posZ - i) || IsOnBomb(new Vector3(posX, 0, posZ - i));
					stopDown = stopDown || IsBrickAt(posX, posZ - i);
					SpawnExplosion(new Vector3(posX, 1, posZ - i), false, bombOwner);
				}
				if(!wallUp && (!stopUp || isPenetrating) && posZ + i <= 42) {
					wallUp = wallUp || IsWallAt(posX, posZ + i) || IsOnBomb(new Vector3(posX, 0, posZ + i));
					stopUp = stopUp || IsBrickAt(posX, posZ + i);
					SpawnExplosion(new Vector3(posX, 1, posZ + i), false, bombOwner);
				}
			}
		}
	}

	private int ReturnRandomPowerup() {
		float rand = UnityEngine.Random.Range(0.0f, 1.0f);
		if(rand <= 0.1f) { //Summon piercing
			return POWERUP_PIERCING;
		} else if(rand <= 0.15f) { //Summon max bomb
			return POWERUP_MAXBOMB;
		} else if(rand <= 0.35f) { //Summon blastRadius
			return POWERUP_BLASTRADIUS;
		} else if(rand <= 0.6f) { //Summon extraBombs
			return POWERUP_EXTRABOMBS;
		} else if(rand <= 0.85f) { //Summon extra speed
			return POWERUP_SPEED;
		} else {
			return 0;
		}
	}
	private void SpawnPowerUp(int puID, Vector3 atPos) {
		if(puID == 0) {
			return;
		}
		GameObject newSpawn = VRCInstantiate(powerUps[puID-1]);
		newSpawn.transform.SetPositionAndRotation(atPos, Quaternion.identity);
		newSpawn.SetActive(true);
		AddPowerup(atPos, puID);
	}

	private void PickUpPowerUp(string powerUp, Players player) {
		int powerUpID = PUStringToValue(powerUp);
		if(player.thisPlayer!=null && player.thisPlayer.playerId == Networking.LocalPlayer.playerId) {
			powerupSound.Play();
		}
		switch(powerUpID) {
			case POWERUP_PIERCING: //More Bombs
				player.isPenPowerup = true;
				break;
			case POWERUP_MAXBOMB: //More Bombs
				player.isInfiBombPowerup = true;
				break;
			case POWERUP_EXTRABOMBS: //More Bombs
				player.bombCountPowerup++;
				if(player.bombCountPowerup > 10) {
					player.bombCountPowerup = 10;
				}
				player.bombsLeft++;
				if(player.bombsLeft > player.bombCountPowerup) {
					player.bombsLeft = player.bombCountPowerup;
				}
				break;
			case POWERUP_BLASTRADIUS: //More bomb range;
				player.blastRadiusPowerup++;
				if(player.blastRadiusPowerup > 10) {
					player.blastRadiusPowerup = 10;
				}
				break;
			case POWERUP_SPEED: //More speed
				player.speedPowerup++;
				if(player.speedPowerup > 15) {
					player.speedPowerup = 15;
				}
				break;
		}
		RemovePowerUp(powerUp);
	}

	private string GetPowerUpNextToPos(Vector3 targetPos) {
		if(!powerUpSpaces.Contains(GetConvertedVector(targetPos))) {
			return "";
		}
		string[] powerUpsInAres = powerUpSpaces.Split(':');
		foreach(string powerUp in powerUpsInAres) {
			if(powerUp.Contains(GetConvertedVector(targetPos))) {
				return powerUp;
			}
		}		
		return "";
	}

	private void AddPowerup(Vector3 atPos, int powerUpId) {
		powerUpSpaces = AddToQueue(PUConvertToString(atPos, powerUpId), powerUpSpaces);
	}
	private void RemovePowerUp(string valueToRemove) {
		powerUpSpaces = RemoveFromQueue(valueToRemove, powerUpSpaces);
		Vector3 remPos = PUStringToVector(valueToRemove);
		int puID = PUStringToValue(valueToRemove);
		Collider[] colObjs = Physics.OverlapSphere(new Vector3(remPos.x, 1, remPos.z), 0.5f, detectable_objects_layerMask);
		if(colObjs != null && colObjs.Length >= 1) {
			foreach(Collider colObj in colObjs) {
				if(colObj.name.Contains(powerUps[puID-1].name)) {
					Destroy(colObj.gameObject);
					break;
				}
			}
		}
	}
	private int PosIDFromVector(Vector3 atPos) {
		return (int)(atPos.x/2) + (int)(atPos.z/2) * 21;
	}
	private string PUConvertToString(Vector3 puVector, int puValue) {
		return GetConvertedVector(puVector) + "+" + puValue.ToString();
	}
	private Vector3 PUStringToVector(string puString) {
		return VectorFromConvertedID(puString.Split('+')[0]);
	}
	private int PUStringToValue(string puString) {
		return int.Parse(puString.Split('+')[1]);
	}

	public void Play_GameGonnaStart() {
		int playSound = UnityEngine.Random.Range(0, gameAboutToStart_audio.Length);
		gameAboutToStart_audio[playSound].Play();
	}
	public void Play_ReadyStart() {
		int playSound = UnityEngine.Random.Range(0, readyStart_audio.Length);
		readyStart_audio[playSound].Play();
	}
	public void Play_HurryUp() {
		int playSound = UnityEngine.Random.Range(0, hurryUp_audio.Length);
		hurryUp_audio[playSound].Play();
	}
	public void Play_Finish() {
		int playSound = UnityEngine.Random.Range(0, finish_audio.Length);
		finish_audio[playSound].Play();
	}

}
