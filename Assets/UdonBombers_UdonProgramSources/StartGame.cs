
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class StartGame : UdonSharpBehaviour
{
	public PlayerList playerCM;

	public override void Interact() {
		playerCM.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "StartTimer");
	}
}
