
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class Mobility : UdonSharpBehaviour
{
	private Vector3 playerVel;
	public GameControl gc;
	private bool wasInArena;
	private int lastSpeed;

  private void Start() {
		playerVel = new Vector3(0.0f, 0.0f, 0.0f);
		Networking.LocalPlayer.SetJumpImpulse(3);
		Networking.LocalPlayer.SetGravityStrength(1);
		lastSpeed = 1;
	}

	private void FixedUpdate() {
		transform.SetPositionAndRotation(Networking.LocalPlayer.GetPosition(), Networking.LocalPlayer.GetRotation());

		bool isInArena = IsPlayerInArena();
		if(wasInArena != isInArena) {
			if(isInArena) {
				Networking.LocalPlayer.SetJumpImpulse(0);
				Networking.LocalPlayer.SetGravityStrength(10);
			} else {
				Networking.LocalPlayer.SetJumpImpulse(3);
				Networking.LocalPlayer.SetGravityStrength(1);
				lastSpeed = 1;
			}
			wasInArena = isInArena;
		}

		if(isInArena) {
			int curSpeed = gc.assignedPlayer != null ? gc.assignedPlayer.speedPowerup : 1;
			if(curSpeed != lastSpeed) {
				Networking.LocalPlayer.SetRunSpeed(4.0f + curSpeed / 3.0f);
				Networking.LocalPlayer.SetWalkSpeed(Networking.LocalPlayer.GetRunSpeed() / 2.0f);
				lastSpeed = curSpeed;
			}

			if(Networking.LocalPlayer.GetVelocity().magnitude > Networking.LocalPlayer.GetRunSpeed()) {
				Networking.LocalPlayer.SetVelocity(Networking.LocalPlayer.GetVelocity().normalized * Networking.LocalPlayer.GetRunSpeed());
			}
		}
		
	}

	public bool IsPlayerInArena() {
		Vector3 playerPos = Networking.LocalPlayer.GetPosition();
		return playerPos.x >= -3.0f && playerPos.x <= 44.0f && playerPos.z >= -3.0f && playerPos.z <= 44.0f;
	}
}
