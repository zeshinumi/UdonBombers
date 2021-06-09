
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class PlayerCollectPlatform : UdonSharpBehaviour
{
	private GameObject redPlat;
	private GameObject greenPlat;
	private GameObject lockPlat;
	private Text playerName;
	[UdonSynced]
	public int playerID;
	private int localPlayerID;
	public VRCPlayerApi player;
	public bool locked;

	private void Start() {
		redPlat = transform.GetChild(0).gameObject;
		greenPlat = transform.GetChild(1).gameObject;
		lockPlat = transform.GetChild(3).gameObject;
		localPlayerID = 0;
		playerName = (Text)transform.GetChild(2).GetChild(0).GetComponent(typeof(Text));
	}

	private void OnTriggerEnter(Collider other) {
		if(!locked && playerID == 0) {
			Networking.SetOwner(Networking.LocalPlayer, gameObject);
			playerID = Networking.LocalPlayer.playerId;
		}
	}

	private void OnTriggerExit(Collider other) {
		if(!locked && Networking.LocalPlayer.playerId == playerID) {
			playerID = 0;
		}
	}

	public override void OnPlayerLeft(VRCPlayerApi player) {
		Reset();
	}

	public void Reset() {
		if(Networking.LocalPlayer.isMaster) {
			Networking.SetOwner(Networking.LocalPlayer, gameObject);
			playerID = 0;
			player = null;
		}
	}

	private void LateUpdate() {
		if(localPlayerID != playerID || (player==null && playerID >= 1)) {
			player = VRCPlayerApi.GetPlayerById(playerID);
			if(player == null) {
				Reset();
			}
		}

		if(locked) {
			lockPlat.SetActive(true);
			greenPlat.SetActive(false);
			redPlat.SetActive(false);
		} else if(player != null && playerID >= 1) {
			playerName.text = player.displayName;
			greenPlat.SetActive(true);
			redPlat.SetActive(false);
			lockPlat.SetActive(false);
			if((player.GetPosition()-transform.position).magnitude >= 2) {
				Reset();
			}
		} else {
			playerName.text = "";
			greenPlat.SetActive(false);
			redPlat.SetActive(true);
			lockPlat.SetActive(false);
		}
	}
}
