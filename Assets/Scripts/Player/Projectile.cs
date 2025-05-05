using UnityEngine;
using Photon.Pun;

public class Projectile : MonoBehaviour{
    public float speed = 20f;
    public int ownerActorNumber; 

    private PhotonView view;


    private void Awake(){
        view = GetComponent<PhotonView>();
    }

    private Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Destroy(gameObject, 5f); // Destroy after 5 seconds
    }
    

    void OnTriggerEnter (){
        if (!view.IsMine) return;
        PhotonNetwork.Destroy(gameObject);
    }

}