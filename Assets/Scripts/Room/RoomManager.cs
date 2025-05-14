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


public class RoomManager : MonoBehaviourPunCallbacks{
    public static RoomManager instance;
    [SerializeField] Transform playerListContent;
    [SerializeField] GameObject playerListItemPrefab;
    public GameObject startButton;
    [SerializeField] TMP_Text roomNameText;
    public GameObject menuHeader;
    public GameObject msgText;

    [Header("Score bar")]
    public Slider scoreSlider;
    public TMP_Text maxScoreText;
    public TMP_Text myScoreText;

    public GameObject gameOverPanel;
    public GameObject exitOrContinuePanel;
    public TMP_Text resultText;


    [Header("Timer")]
    public float timeRemaining = 310f; 
    public bool timerIsRunning = false;
    public TMP_Text timerText;
    void Update(){
        if (timerIsRunning){
            if (timeRemaining > 0){
                timeRemaining -= Time.deltaTime;
                UpdateTimerDisplay(timeRemaining);
            }else{
                timeRemaining = 0;
                timerIsRunning = false;
                UpdateTimerDisplay(timeRemaining);
                Debug.Log("Timer finished!");
            }
        }
    }

    void UpdateTimerDisplay(float timeToDisplay){
        timeToDisplay += 1f; // Optional for display smoothness
        int minutes = Mathf.FloorToInt(timeToDisplay / 60f);
        int seconds = Mathf.FloorToInt(timeToDisplay % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void OpenPanel(string panelName){
        switch(panelName){
            case "GameOverPanel":
                gameOverPanel.SetActive(true);
                exitOrContinuePanel.SetActive(false);
                break;
            case "ExitOrContinuePanel":
                exitOrContinuePanel.SetActive(true);
                gameOverPanel.SetActive(false);
                break;
            case "Continue":
                exitOrContinuePanel.SetActive(false);
                gameOverPanel.SetActive(false);
                break;
            default:
                Debug.LogError($"Unknown panel name: {panelName}");
                break;
        }
    }

  

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
        Debug.LogError($"Disconnected from Photon: {cause}");
    }
    public override void OnJoinedLobby(){
        Debug.Log($"Joined Lobby:: {PhotonNetwork.CurrentLobby.Name}");
        Debug.Log($"USER Joined Lobby:: {SolanaManager.instance.playerName}");
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
            PhotonNetwork.NickName = SolanaManager.instance.playerName ?? "unknown";   
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

        maxScoreText.text = scoreSlider.maxValue.ToString();
        myScoreText.text = bid.ToString();
        
        Debug.Log(
            "Max Score Slider Value: " + scoreSlider.maxValue + 
            " Players: " + PhotonNetwork.CurrentRoom.PlayerCount +
            " Bid: " + bid
            );
    }
}
