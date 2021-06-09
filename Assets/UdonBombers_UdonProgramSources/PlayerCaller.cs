
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PlayerCaller : UdonSharpBehaviour {
	public PlayerList pList;
	private int myNum;
	[HideInInspector]
	public VRCPlayerApi player;
	[HideInInspector]
	public bool amInGame;

	public void SetUpCaller(int newNum, VRCPlayerApi newPlayer) {
		myNum = newNum;
		player = newPlayer;
	}
	public void SetNumber(int newNum) {
		myNum = newNum;
	}
	public int GetMyNum() {
		return myNum;
	}

	public void IAmInTheGame() {
		amInGame = true;
		pList.joinButton.UpdatePeds();
	}
	public void IAmNotInTheGame() {
		amInGame = false;
		pList.joinButton.UpdatePeds();
	}
}
