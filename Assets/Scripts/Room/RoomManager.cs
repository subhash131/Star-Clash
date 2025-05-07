using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.IO;
using Photon.Realtime;
using System;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using Solana.Unity.Metaplex.Auctioneer.Types;


public class RoomManager : MonoBehaviourPunCallbacks{
    public static RoomManager instance;
    [SerializeField] Transform playerListContent;
    [SerializeField] GameObject playerListItemPrefab;
    public GameObject startButton;
    [SerializeField] TMP_Text roomNameText;
    public GameObject menuHeader;
    public GameObject msgText;
    public Slider scoreSlider;
  

    void Awake(){
        if(instance == null){
            instance = this;
            DontDestroyOnLoad(gameObject);            
        }
        else{
            Destroy(gameObject);
        }  
    }

    void Start(){
        PhotonNetwork.ConnectUsingSettings();
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
        PhotonNetwork.NickName = SolanaManager.instance.playerName ?? "unknown";   
    }

    public void JoinRoom(string roomName){

        ExitGames.Client.Photon.Hashtable customProperties = new(){
            { "BidAmount", int.Parse(roomName) },
        };
        try{
            RoomOptions roomOptions = new(){
                IsVisible = true,
                IsOpen = true,
                MaxPlayers = 10,
                CustomRoomProperties =  customProperties,
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
        GameObject playerItem = Instantiate(playerListItemPrefab, playerListContent);
        playerItem.SetActive(true);
        playerItem.GetComponent<PlayerListItem>().SetUp(newPlayer);
    }
    public  void Shoot(){
        Debug.Log($"Shoot:");
    }

    public override void OnJoinedRoom(){
        MenuManager.instance.OpenMenu("RoomMenu");
        roomNameText.text = $"Bidding {PhotonNetwork.CurrentRoom.Name} coins";

        Player[] players = PhotonNetwork.PlayerList;

        foreach(Transform child in playerListContent){
            Destroy(child.gameObject);
        }

        for(int i=0; i<players.Count(); i++){
            GameObject playerItem = Instantiate(playerListItemPrefab, playerListContent);
            playerItem.SetActive(true);
            playerItem.GetComponent<PlayerListItem>().SetUp(players[i]);
         } 

         startButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public void LeaveRoom(){
        PhotonNetwork.LeaveRoom();
        MenuManager.instance.OpenMenu("ContractMenu");
    }

    public override void OnMasterClientSwitched(Player newMasterClient){
       startButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public void StartGame() {
        if (PhotonNetwork.IsMasterClient) {
            menuHeader.SetActive(false);
            msgText.SetActive(false);
            photonView.RPC("StartGameRPC", RpcTarget.All);
        }
    }
    public void QuitGame() {
        //TODO::
        menuHeader.SetActive(true);
        msgText.SetActive(true);
    }

    [PunRPC]
    private void StartGameRPC(){
        Debug.Log("Starting game for all clients!");
        int bid = PhotonNetwork.CurrentRoom.CustomProperties["BidAmount"] != null ? (int)PhotonNetwork.CurrentRoom.CustomProperties["BidAmount"] : 0;   

        // SolanaManager.instance.messageText.text = $"Bidding {bid} coins";
       

        MenuManager.instance.OpenMenu("GameMenu");
        PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs","PlayerController"),Vector3.zero, Quaternion.identity);
        scoreSlider.maxValue = PhotonNetwork.CurrentRoom.PlayerCount * bid;
        scoreSlider.value = bid;
        Debug.Log(
            "Max Score Slider Value: " + scoreSlider.maxValue + 
            " Players: " + PhotonNetwork.CurrentRoom.PlayerCount +
            " Bid: " + bid
            );
    }
}
