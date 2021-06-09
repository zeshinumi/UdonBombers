
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System.Collections.Generic;

public class GetPlayersFromCollider : UdonSharpBehaviour
{
	[UdonSynced]
	private string playersInCubeString;

	private void Start() {
		if(Networking.IsMaster) {
			playersInCubeString = "";
		}
	}

	private void OnTriggerEnter(Collider other) {		
		AddPlayerById(Networking.LocalPlayer.playerId);
	}

	private void OnTriggerExit(Collider other) {
		RemovePlayerById(Networking.LocalPlayer.playerId);
	}

	private string ConvertId(int playerId) {
		if(playerId < 10) {
			return "0" + playerId.ToString();
		} else { 
			return playerId.ToString();
		}
	}

	public void AddPlayerById(int targetId) {
		string id = ConvertId(targetId);
		if(playersInCubeString.Contains(id)) {
			return;
		}
		Debug.Log("Adding " + id + " to " + gameObject.name);
		Networking.SetOwner(Networking.LocalPlayer, gameObject);
		if(playersInCubeString == "") {
			playersInCubeString += id;
		} else {
			playersInCubeString += ":" + id;
		}
	}

	public void RemovePlayerById(int targetId) {
		string id = ConvertId(targetId);
		if(!playersInCubeString.Contains(id)) {
			return;
		}
		Debug.Log("Removing " + id + " from " + gameObject.name);
		Networking.SetOwner(Networking.LocalPlayer, gameObject);
		string idListToSet = playersInCubeString.Replace(id, "").Replace("::", ":");
		Debug.Log("New list: " + idListToSet + " with a length of " + idListToSet.Length);
		if(idListToSet.Length <= 5) {
			idListToSet = idListToSet.Replace(":", "");
		}
		playersInCubeString = idListToSet;
	}


	public void ClearList() {
		Debug.Log("Clearing list of " + gameObject.name);
		Networking.SetOwner(Networking.LocalPlayer, gameObject);
		playersInCubeString = "";
	}

	public VRCPlayerApi[] GetPlayersInCollider() {
		Debug.Log("Current list for " + gameObject.name + ": " + playersInCubeString);
		Debug.Log("test script");
		if(playersInCubeString == "") {
			Debug.Log("No players in here");
			return null;
		}
		string[] playerIds = playersInCubeString.Split(':');
		if(playerIds.Length == 0) {
			return null;
		} else {
			VRCPlayerApi[] players = new VRCPlayerApi[playerIds.Length];
			for(int i = 0; i < playerIds.Length; i++) {
				players[i] = VRCPlayerApi.GetPlayerById(int.Parse(playerIds[i]));
			}
			return players;
		}
	}

}
