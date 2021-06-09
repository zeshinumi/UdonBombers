
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

[AddComponentMenu("Udon Sharp/Utilities/Interact Toggle")]
public class CheckPlayers : UdonSharpBehaviour
{
	public UdonBehaviour theGame;
	public Text timerText;
	private PlayerCollectPlatform[] platforms;
	public Animator bombAnim;
	[UdonSynced]
	private bool isDoingTimer;
	private bool hasLockedPads;
	private float startLockedTimer;
	private VRCPlayerApi[] lockedInPlayers;

	private float startTimerTime;

	private void Start() {
		platforms = GetComponentsInChildren<PlayerCollectPlatform>();
		timerText.text = "";
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
					isDoingTimer = false;
				}
			} else {
				timerText.text = ((int)(10 - (Time.time - startTimerTime))).ToString();
			}
		} else {
			timerText.text = "";
			if(hasLockedPads && Time.time - startTimerTime > 13) {
				SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UnLockPads");
				hasLockedPads = false;
			}
		}
	}

	public void LockPads() {
		foreach(PlayerCollectPlatform platform in platforms) {
			platform.locked = true;
		}
	}

	public void UnLockPads() {
		foreach(PlayerCollectPlatform platform in platforms) {
			platform.locked = false;
		}
	}

	public void StartTimer() {
		theGame.SendCustomEvent("Play_GameGonnaStart");
		if(Networking.IsOwner(gameObject)) {
			isDoingTimer = true;
		}
		hasLockedPads = false;
		startTimerTime = Time.time;
		bombAnim.Play("BombPulsate");
	}

	public override void Interact() {
		bool isGameActive = (bool)theGame.GetProgramVariable("syncedIsGameActive");
		if(!isGameActive && !isDoingTimer) {
			SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "StartTimer");
		}
	}

	public void Reset() {
		foreach(PlayerCollectPlatform platform in platforms) {
			platform.Reset();
		}
	}

	public VRCPlayerApi[] GetPlayers() {
		VRCPlayerApi[] players = new VRCPlayerApi[12];
		int i = 0;
		foreach(PlayerCollectPlatform platform in platforms) {
			if(platform.player!=null && platform.playerID != 0) {
				players[i] = platform.player;
				i++;
			}
		}
		return players;
	}


}
