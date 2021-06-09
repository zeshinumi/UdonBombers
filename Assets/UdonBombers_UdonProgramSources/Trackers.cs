
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Trackers : UdonSharpBehaviour {
	public int head1Mid2Feet3Left4Right5;
	private VRCPlayerApi currentPlayer;

	void Start() {
		currentPlayer = Networking.LocalPlayer;
	}

	private void FixedUpdate() {
		switch(head1Mid2Feet3Left4Right5) {
			case 1:
				TrackHead();
				break;
			case 2:
				TrackMid();
				break;
			case 3:
				TrackFeet();
				break;
			case 4:
				TrackLeftHand();
				break;
			case 5:
				TrackRightHand();
				break;
		}
	}

	private void TrackHead() {
		VRCPlayerApi.TrackingData tracked = currentPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
		transform.SetPositionAndRotation(tracked.position, tracked.rotation);
	}
	private void TrackLeftHand() {
		VRCPlayerApi.TrackingData tracked = currentPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
		if(currentPlayer.IsUserInVR()) {
			transform.SetPositionAndRotation(tracked.position, tracked.rotation);
			transform.Rotate(new Vector3(0, -22, 0));
		} else {
			VRCPlayerApi.TrackingData trackedHead = currentPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
			transform.SetPositionAndRotation(tracked.position, trackedHead.rotation);
			transform.Rotate(new Vector3(0, -90, 0));
		}
	}
	private void TrackRightHand() {
		VRCPlayerApi.TrackingData tracked = currentPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
		if(currentPlayer.IsUserInVR()) {
			transform.SetPositionAndRotation(tracked.position, tracked.rotation);
			transform.Rotate(new Vector3(0, -22, 0));
		} else {
			VRCPlayerApi.TrackingData trackedHead = currentPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
			transform.SetPositionAndRotation(tracked.position, trackedHead.rotation);
			transform.Rotate(new Vector3(0, -90, 0));
		}
	}
	private void TrackMid() {
		VRCPlayerApi.TrackingData head = currentPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
		Vector3 mid = (head.position + currentPlayer.GetPosition()) / 2;
		transform.SetPositionAndRotation(mid, currentPlayer.GetRotation());
	}
	private void TrackFeet() {
		transform.SetPositionAndRotation(currentPlayer.GetPosition(), currentPlayer.GetRotation());
	}
}
