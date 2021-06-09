
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CubeManager : UdonSharpBehaviour
{
	public Transform respawnLoc;
	public GameObject damagePrefab;
	private GetPlayersFromCollider[] cubes;
	private float lastTime;
	private float randTime;

	private void Start() {
		cubes = GetComponentsInChildren<GetPlayersFromCollider>();
		/*Networking.LocalPlayer.CombatSetup();
		Networking.LocalPlayer.CombatSetDamageGraphic(damagePrefab);
		Networking.LocalPlayer.CombatSetRespawn(true, 5, respawnLoc);
		Networking.LocalPlayer.CombatSetMaxHitpoints(1);
		Networking.LocalPlayer.CombatSetCurrentHitpoints(1);*/

		lastTime = Time.time;
		randTime = Random.Range(1, 5);
	}

	private void Update() {
		if(Time.time - lastTime > randTime && false) {
			/*int hb = Random.Range(0, cubes.Length);
			Debug.Log("Checking " + cubes[hb].gameObject.name);
			VRCPlayerApi[] playersInCube = cubes[hb].GetPlayersInCollider();
			if(playersInCube != null) {
				foreach(VRCPlayerApi player in playersInCube) {
					Debug.Log(player.displayName + " is Hit!");
					//player.CombatSetCurrentHitpoints(-1);
				}
				cubes[hb].ClearList();
			}*/
			lastTime = Time.time;
			randTime = Random.Range(1, 5);
		}
	}

	public VRCPlayerApi GetCurrentMaster() {
		return Networking.GetOwner(gameObject);
	}
}
