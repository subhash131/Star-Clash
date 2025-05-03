using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.IO;


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

}
