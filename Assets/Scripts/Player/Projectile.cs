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
                ReduceScore(1, playerView);
                IncreaseScore(1, PhotonNetwork.CurrentRoom.GetPlayer(ownerActorNumber));
            }
        }

        if(view.IsMine)
            PhotonNetwork.Destroy(gameObject);
    }

    void ReduceScore(int score, PhotonView playerView)
    {
        if (!PhotonNetwork.IsMasterClient) return; // Only master client updates scores

        Player hitPlayer = playerView.Owner;
        if (hitPlayer != null)
        {
            // Get current score
            hitPlayer.CustomProperties.TryGetValue("Score", out object currentScore);
            int newScore = currentScore != null ? (int)currentScore - score : -score;
            newScore = Mathf.Max(0, newScore); // Prevent negative scores

            // Update score in custom properties
            ExitGames.Client.Photon.Hashtable scoreUpdate = new ExitGames.Client.Photon.Hashtable
            {
                { "Score", newScore }
            };
            hitPlayer.SetCustomProperties(scoreUpdate);

            Debug.Log($"Reduced score for player {hitPlayer.NickName} to {newScore}");
        }
    }

    void IncreaseScore(int score, Player player)
    {
        if (!PhotonNetwork.IsMasterClient) return; // Only master client updates scores

        if (player != null)
        {
            // Get current score
            player.CustomProperties.TryGetValue("Score", out object currentScore);
            int newScore = currentScore != null ? (int)currentScore + score : score;

            // Update score in custom properties
            ExitGames.Client.Photon.Hashtable scoreUpdate = new ExitGames.Client.Photon.Hashtable
            {
                { "Score", newScore }
            };
            player.SetCustomProperties(scoreUpdate);

            Debug.Log($"Increased score for player {player.NickName} to {newScore}");
        }
    }
}