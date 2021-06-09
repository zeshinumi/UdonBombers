
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class JoinGameButton : UdonSharpBehaviour
{
	public PlayerList pList;
	public Transform pedsParent;
	public Material inactiveMat;
	public Material activeMat;
	public Material redMat;
	private MeshRenderer[] pedMatList;
	private Text[] pedTextList;
	private bool hasStarted, isLocked;
	private MeshRenderer thisMesh;
	private Text thisText;

	private void Start() {
		pedMatList = new MeshRenderer[pedsParent.childCount];
		pedTextList = new Text[pedsParent.childCount];
		for(int i = 0; i < pedsParent.childCount; i++) {
			pedMatList[i] = pedsParent.GetChild(i).GetComponent<MeshRenderer>();
			pedTextList[i] = pedsParent.GetChild(i).GetChild(0).GetChild(0).GetComponent<Text>();
		}
		hasStarted = true;
		thisMesh = gameObject.GetComponent<MeshRenderer>();
		thisText = transform.GetChild(0).GetChild(0).GetComponent<Text>();
	}

	public void UpdatePeds() {
		if(!hasStarted) {
			return;
		}
		VRCPlayerApi[] inGameList = pList.GetInGamePlayerList();
		for(int i = 0; i < pedMatList.Length; i++) {
			if(i < inGameList.Length) {
				pedMatList[i].material = activeMat;
				pedTextList[i].text = inGameList[i].displayName;
			} else {
				pedMatList[i].material = inactiveMat;
				pedTextList[i].text = "";
			}
		}
	}

	public override void Interact() {
		if(isLocked) {
			return;
		}
		if(pList.GetMyPC().amInGame) {
			pList.RemoveMeFromGame();
			thisMesh.material = redMat;
			thisText.text = "Join Game";
		} else {
			pList.AddMeToGame();
			thisMesh.material = activeMat;
			thisText.text = "Leave Game";
		}
	}

	public void LockPads() {
		isLocked = true;
		thisMesh.material = inactiveMat;
		thisText.text = "Join Locked";
	}

	public void UnLockPads() {
		isLocked = false;
		if(pList.GetMyPC().amInGame) {
			thisMesh.material = activeMat;
			thisText.text = "Leave Game";
		} else {
			thisMesh.material = redMat;
			thisText.text = "Join Game";
		}
		pList.hasLockedPads = false;
	}
}
