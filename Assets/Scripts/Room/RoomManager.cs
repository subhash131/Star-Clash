using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.IO;
using Photon.Realtime;
using System;


public class RoomManager : MonoBehaviourPunCallbacks{
    public static RoomManager instance;

    void Awake(){
        if(instance == null){
            instance = this;
        }
        else{
            Destroy(gameObject);
        }  
    }

    void Start(){
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnEnable() {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable() {
        base.OnDisable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode){
        // if(scene.buildIndex == 2){
        //     PhotonNetwork.Instantiate(Path.Combine("PhotonPrefab","PlayerController"),Vector3.zero, Quaternion.identity);
        // }
    }

    public override void OnConnectedToMaster(){
        Debug.Log("Connected to Photon Master Server!");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene=true;
    }

    public override void OnDisconnected(DisconnectCause cause){
        Debug.LogError($"   Disconnected from Photon: {cause}");
    }
    public override void OnJoinedLobby(){
        Debug.Log($"Joined Lobby:: {PhotonNetwork.CurrentLobby.Name}");
        PhotonNetwork.NickName = SolanaManager.instance.userAccount?.Username ?? "subhash";   
    }

    public void JoinRoom(string roomName){
        try{
            RoomOptions roomOptions = new(){
                IsVisible = true,
                IsOpen = true,
                MaxPlayers = 4
            };
            TypedLobby typedLobby = new(roomName, LobbyType.Default);
            PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, typedLobby);
            Debug.Log($"Joined Room:: {roomName}");
        }catch(Exception e){
            Debug.LogError($"Error joining room: {e}");
        }
    }   

    public override void OnPlayerEnteredRoom(Player newPlayer){
        Debug.Log($"Player Joined room: {newPlayer.NickName}");
    }
    public  void Shoot(){
        Debug.Log($"Shoot:");
    }
}
