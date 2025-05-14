using System.IO;
using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;
using ExitGames.Client.Photon;

public class PlayerController : MonoBehaviourPunCallbacks{
    private PhotonView view;
    private GameObject controller;

    void Awake(){
        view = GetComponent<PhotonView>();
    }
    private void Start(){
        if (view.IsMine){
            CreateController();
        }
    }

    void CreateController(){
        // Base spawn position
        Vector3 basePosition = new Vector3(5f, 1f, 5f);

        // Calculate offset based on ActorNumber (ActorNumber starts at 1)
        float offset = (PhotonNetwork.LocalPlayer.ActorNumber - 1) * 20f; // 20 units apart per player
        Vector3 spawnPosition = basePosition + new Vector3(offset, 0f, 0f);

        Hashtable props = new(){
                { "Score", PhotonNetwork.CurrentRoom.CustomProperties["BidAmount"] },
                { "PlayerName", SolanaManager.instance.playerName },
                { "PlayerID", PhotonNetwork.LocalPlayer.ActorNumber },
            };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        controller = PhotonNetwork.Instantiate(
            Path.Combine("PhotonPrefabs", "Player"),
            spawnPosition,
            Quaternion.identity,
            0,
            new object[] { view.ViewID }
        );
        RoomManager.instance.timerIsRunning = true;
        Debug.Log("Player instantiated for ViewID: " + view.ViewID);
    }

}