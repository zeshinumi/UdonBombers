
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class PlayerCollecterButton : UdonSharpBehaviour
{	
	[UdonSynced]
	private int MyPlayerID;
	private int localPlayerID;
	private PlayerCollectorMaster masterC;
	private int pedID;
	private MeshRenderer myMeshRend;
	private int lockedInPlayerID;
	private Text displayName;
	private bool hasLockedDown;
	private bool isOwnedByMaster;
	private bool needToClaimObject;
	private bool needToClearObject;
	private float lastTimeTouchedButton;

	private void Start() {
		pedID = int.Parse(gameObject.name.Replace("Platform (", "").Replace(")", "")) - 1;
		masterC = transform.parent.GetComponent<PlayerCollectorMaster>();
		myMeshRend = gameObject.GetComponent<MeshRenderer>();
		displayName = transform.GetChild(0).GetChild(0).GetComponent<Text>();
		displayName.text = "";
	}

	public override void Interact() {
		if(masterC.CanITakePed(pedID, Networking.LocalPlayer.playerId)) {
			if(!Networking.LocalPlayer.IsOwner(gameObject)) {
				needToClaimObject = true;
				lastTimeTouchedButton = Time.time;
				Networking.SetOwner(Networking.LocalPlayer, gameObject);
			}			
			if(Networking.LocalPlayer.isMaster) {
				isOwnedByMaster = true;
			}
			MyPlayerID = Networking.LocalPlayer.playerId;
		} else if(MyPlayerID == Networking.LocalPlayer.playerId) {
			MyPlayerID = 0;
			if(Networking.LocalPlayer.isMaster) {
				isOwnedByMaster = false;
			}
		}
	}

	public void LockInPlayerID() {
		lockedInPlayerID = localPlayerID;
	}
	public int GetLockedInPlayerID() {
		return lockedInPlayerID;
	}

	public override void OnPlayerLeft(VRCPlayerApi player) {
		if(Networking.LocalPlayer.isMaster && player.playerId < Networking.LocalPlayer.playerId && Networking.LocalPlayer.playerId == localPlayerID) {
			isOwnedByMaster = true;
		}else if(Networking.LocalPlayer.IsOwner(gameObject) && localPlayerID == player.playerId) {
			MyPlayerID = 0;
			isOwnedByMaster = false;
			needToClearObject = true;
			lastTimeTouchedButton = Time.time;
		}
	}

	private bool DoIOwnThisObject() {
		return (isOwnedByMaster && Networking.LocalPlayer.isMaster) || (!Networking.LocalPlayer.isMaster && Networking.LocalPlayer.IsOwner(gameObject));
	}

	private void LateUpdate() {
		if(!masterC.hasDoneStart)
			return;
		if(masterC.isLocked) {
			if(!hasLockedDown) {
				hasLockedDown = true;
				lockedInPlayerID = localPlayerID;
				if(localPlayerID == 0) {
					myMeshRend.material = masterC.greyPedMat;
				}
			}
		} else {
			if(hasLockedDown) {
				myMeshRend.material = masterC.redPedMat;
				hasLockedDown = false;
			}
			if(needToClaimObject && Time.time - lastTimeTouchedButton > 1.0f && MyPlayerID != Networking.LocalPlayer.playerId && DoIOwnThisObject()) {
				MyPlayerID = Networking.LocalPlayer.playerId;
				needToClaimObject = false;
			}
			if(needToClearObject && Time.time - lastTimeTouchedButton > 1.0f && Networking.IsOwner(gameObject)) {
				MyPlayerID = 0;
				isOwnedByMaster = false;
				needToClearObject = false;
			}

			if(MyPlayerID != localPlayerID) {
				localPlayerID = MyPlayerID;
				masterC.pedPlayersIDs[pedID] = MyPlayerID;
				if(localPlayerID == 0) {
					myMeshRend.material = masterC.redPedMat;
					displayName.text = "";
				} else {
					VRCPlayerApi player = VRCPlayerApi.GetPlayerById(localPlayerID);
					if(player == null) {
						if(Networking.IsOwner(gameObject)) {
							MyPlayerID = 0;
						}
						myMeshRend.material = masterC.redPedMat;
						displayName.text = "";
					} else {
						myMeshRend.material = masterC.greenPedMat;
						displayName.text = player.displayName;
					}
				}
			}
			if(masterC.pedPlayersIDs != null && masterC.pedPlayersIDs[pedID] != MyPlayerID && Networking.LocalPlayer.IsOwner(gameObject)) {
				MyPlayerID = masterC.pedPlayersIDs[pedID];
			}
		}
	}

}
