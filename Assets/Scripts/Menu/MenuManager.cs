using System.IO;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviourPunCallbacks{
    public static MenuManager instance;

    public GameObject menuCamera;
    public Menu[] menus;

    void Awake() {
        instance = this;
    }

    public void OpenMenu(string menuName){
        for(int i=0; i<menus.Length; i++){
            if(menus[i].menuName == menuName){
                menus[i].Open();
            }else if(menus[i].isOpen){
                CloseMenu(menus[i]);
            }
        }
    }

    public void OpenMenu(Menu menu){
        for(int i=0; i<menus.Length; i++){
            if(menus[i].isOpen){
                CloseMenu(menus[i]);
            }
        }
        menu.Open();
    }

    public void CloseMenu(Menu menu){
        menu.Close();
    }
    public void CloseAllMenu(){
          for(int i=0; i<menus.Length; i++){
            menus[i].Close();
        }
    }
}
