using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections;
using System.Collections.Generic;


public class PlayerListItem : MonoBehaviourPunCallbacks{
    public TMP_Text playerName;
    Player player;

    public void SetUp(Player _player){
        player = _player;
        playerName.text = _player.NickName;
    }

    public override void OnPlayerLeftRoom(Player otherPlayer){
        if(player == otherPlayer){
            Destroy(gameObject);
        }
    }

    public override void OnLeftRoom(){
        Destroy(gameObject);
    }
}
