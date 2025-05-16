using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Projectile : MonoBehaviourPunCallbacks
{
    public float speed = 20f;
    public int ownerActorNumber;

    private PhotonView view;
    private Rigidbody rb;

    private void Awake()
    {
        view = GetComponent<PhotonView>();
    }   

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Destroy(gameObject, 5f); // Destroy after 5 seconds
    }

    void OnTriggerEnter(Collider other){
        if (other.gameObject.CompareTag("Player")) {
            // Get the PhotonView of the hit player
            PhotonView playerView = other.GetComponent<PhotonView>();
            Debug.Log("playerView ::" + playerView); 
            if (playerView != null && playerView.OwnerActorNr != ownerActorNumber) {
                UpdateScores(1, playerView, PhotonNetwork.CurrentRoom.GetPlayer(ownerActorNumber));
            }
        }

        if(view.IsMine)
            PhotonNetwork.Destroy(gameObject);
    }

    void UpdateScores (int score, PhotonView enemyView, Player shooter){
        Player hitPlayer = enemyView.Owner;
        if(hitPlayer == null || shooter == null){
            Debug.LogError("player is null");
            return;
        }
        // Reduce score for the hit player
        hitPlayer.CustomProperties.TryGetValue("Score", out object hitPlayerCurrentScore);
        int hitPlayerNewScore = hitPlayerCurrentScore != null ? (int)hitPlayerCurrentScore - score : -score;
        hitPlayerNewScore = Mathf.Max(0, hitPlayerNewScore); // Prevent negative scores

        if(hitPlayerNewScore == 0){
            // Player has no score left, handle accordingly
            Debug.Log($"Player {hitPlayer.NickName} has reached zero score.");
            PhotonNetwork.Destroy(enemyView.gameObject); // Destroy the player object
            // RoomManager.instance.gameOverPanel.SetActive(true); // Show game over panel
        }
        // Update score in custom properties
        ExitGames.Client.Photon.Hashtable scoreUpdate = new(){
            { "Score", hitPlayerNewScore }
        };
        hitPlayer.SetCustomProperties(scoreUpdate);

        Debug.Log($"Reduced score for player {hitPlayer.NickName} to {hitPlayerNewScore}");
        // Increase score for the shooter
        shooter.CustomProperties.TryGetValue("Score", out object shooterCurrentScore);
        int shooterNewScore = shooterCurrentScore != null && hitPlayerNewScore !=0 ? (int)shooterCurrentScore + score : score;

        // Update score in custom properties
        ExitGames.Client.Photon.Hashtable shooterUpdatedScore = new (){
            { "Score", shooterNewScore }
        };
        shooter.SetCustomProperties(shooterUpdatedScore);

        Debug.Log($"Increased score for player {shooter.NickName} to {shooterNewScore}");
        
    }

 
}