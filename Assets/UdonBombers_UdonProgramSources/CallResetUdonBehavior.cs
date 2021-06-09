
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CallResetUdonBehavior : UdonSharpBehaviour
{
	public UdonBehaviour targetBehavior;

	public override void Interact() {
		targetBehavior.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Reset");
	}
}
