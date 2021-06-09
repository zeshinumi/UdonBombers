
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Players : UdonSharpBehaviour
{
	public VRCPlayerApi thisPlayer;
	public bool isDead;
	private GameControl controller;
	public bool isActive;
	private bool isClicked;
	public Vector3 lastSpaceIn;
	private Vector3 noSpace = new Vector3(-1.0f, -1.0f, -1.0f);
	public int blastRadiusPowerup;
	public int bombCountPowerup;
	public int speedPowerup;
	public bool isInfiBombPowerup;
	public bool isPenPowerup;
	public bool justLaidBombNIsOnBomb;
	public bool hasStartedGame;
	public Transform startPos;
	public int thisPlayerID;
	public bool isReviving;

	private float lastLaidBomb;
	public bool isPatron;
	public int bombsLeft;

	public void Initialize() {
		controller = transform.parent.GetComponent<GameControl>();
		lastSpaceIn = noSpace;
		isDead = true;
	}

	public string PowerUpOutput() {
		return blastRadiusPowerup.ToString() + ":" + bombCountPowerup.ToString() + ":" + speedPowerup.ToString() + ":" + isInfiBombPowerup.ToString() + ":" + isPenPowerup.ToString();
	}

	public void SetPlayer(VRCPlayerApi newPlayer) {
		thisPlayer = newPlayer;
		thisPlayerID = newPlayer.playerId;
		isPatron = controller.IsPatron(thisPlayer.displayName);
		TurnOn();
	}

	public void LateUpdate() {
		if(isActive && thisPlayer != null && thisPlayer == Networking.LocalPlayer) {
			if(IsPlayerInArena()) {
				if(isReviving && IsInAliveArea()) {
					isReviving = false;
				}
				if(bombsLeft != bombCountPowerup && Time.time - lastLaidBomb >= 4) {
					bombsLeft = bombCountPowerup;
				}
				if(lastSpaceIn.Equals(noSpace)) {
					lastSpaceIn = GetConvertedPos();
				}
				if(!controller.IsOnBomb(GetConvertedPos())) {
					justLaidBombNIsOnBomb = false;
					if(Networking.LocalPlayer.IsUserInVR()) {
						if(!isClicked && (Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger") >= 0.9f)) {
							LayBomb();
							isClicked = true;
						} else if(isClicked && (Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger") <= 0.1f)) {
							isClicked = false;
						}
					} else {
						if(Input.GetMouseButtonDown(0)) {
							LayBomb();
						}
					}
				}				
			}else if(hasStartedGame) {
				isDead = true;
				hasStartedGame = false;
			}
		}
		if(isActive && !hasStartedGame && thisPlayer!=null && (thisPlayer.GetPosition()-startPos.position).magnitude<=1.0f) {
			controller.SetCollidableObjectsInWay(true);
			hasStartedGame = true;
		} 
	}

	public bool ShouldBeDead() {
		return hasStartedGame && !isReviving && !IsInAliveArea();
	}

	private void LayBomb() {
		if(bombsLeft > 0) {
			if(bombsLeft == bombCountPowerup) {
				lastLaidBomb = Time.time;
			}
			justLaidBombNIsOnBomb = true;
			if(isInfiBombPowerup && bombCountPowerup == bombsLeft) {
				SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Summon_Big_Bomb");
			} else {
				SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Summon_Bomb");
			}
			bombsLeft--;
		}
	}

	public override void OnPlayerLeft(VRCPlayerApi player) {
		if(player.playerId == thisPlayerID) {
			TurnOff();
		}
	}

	public void TurnOn() {
		hasStartedGame = false;
		isActive = true;
		isDead = false;
		blastRadiusPowerup = 1;
		isPenPowerup = false;
		isInfiBombPowerup = false;
		bombCountPowerup = 1;
		speedPowerup = 1;
		isClicked = true;
	}

	public void SetCurrentPos() {
		lastSpaceIn = GetConvertedPos();
	}

	public void TurnOff() {
		isActive = false;
		isDead = true;
		hasStartedGame = false;
		thisPlayerID = 0;
	}

	public bool IsPlayerInArena() {
		if(thisPlayer == null) {
			return false;
		}
		Vector3 playerPos = thisPlayer.GetPosition();
		return playerPos.x >= -3.0f && playerPos.x <= 44.0f && playerPos.z >= -3.0f && playerPos.z <= 44.0f;
	}
	public bool IsInAliveArea() {
		if(thisPlayer == null) {
			return false;
		}
		Vector3 playerPos = thisPlayer.GetPosition();
		return playerPos.x >= -1.0f && playerPos.x <= 41.0f && playerPos.z >= -1.0f && playerPos.z <= 41.0f;
	}

	public string GetThisPlayerPos() {
		if(thisPlayer == null) {
			return "No Player";
		} else {
			return thisPlayer.GetPosition().ToString();
		}
	}

	public int GetPosID() {
		if(thisPlayer == null) {
			return -1;
		} else {
			return (int)thisPlayer.GetPosition().x + (int)thisPlayer.GetPosition().z * 21;
		}
	}

	public void KillPlayer() {
		controller.KillPlayer(this);
	}

	public Vector3 GetConvertedPos() {
		if(thisPlayer == null) {
			return noSpace;
		}
		int newX;
		if((int)thisPlayer.GetPosition().x % 2 != 0) {
			newX = (int)thisPlayer.GetPosition().x + 1;
		}else{
			newX = (int)thisPlayer.GetPosition().x;
		}
		if(newX < 0) {
			newX = 0;
		}else if(newX > 40) {
			newX = 40;
		}

		int newZ = (int)thisPlayer.GetPosition().z;
		if((int)thisPlayer.GetPosition().z % 2 != 0) {
			newZ = (int)thisPlayer.GetPosition().z + 1;
		} else {
			newZ = (int)thisPlayer.GetPosition().z;
		}
		if(newZ < 0) {
			newZ = 0;
		} else if(newZ > 40) {
			newZ = 40;
		}
		return new Vector3(newX, 1, newZ);
	}

	public void Summon_Bomb() {
		if(thisPlayer == null) {
			return;
		}
		controller.CreateBomb(GetConvertedPos(), isPenPowerup, blastRadiusPowerup, isDead, thisPlayer.GetPosition(), isPatron, thisPlayerID);
	}
	public void Summon_Big_Bomb() {
		if(thisPlayer == null) {
			return;
		}
		controller.CreateBomb(GetConvertedPos(), false, 21, isDead, thisPlayer.GetPosition(), isPatron, thisPlayerID);
	}

	public void RevivePlayer() {
		isDead = false;
		isReviving = true;
		if(Networking.LocalPlayer == thisPlayer) {			
			Networking.LocalPlayer.TeleportTo(startPos.position, startPos.rotation);
		}
		controller.StopEndGameTimer();
	}

}
