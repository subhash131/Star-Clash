using UnityEngine;
using UnityEngine.EventSystems;

public class SwipeInput : MonoBehaviour, IDragHandler{

    public static SwipeInput instance;
    
    public MyPlayer player;


    void Awake(){
        instance = this;
    }

    public void OnDrag(PointerEventData eventData){
        if (player != null) {
            player.OnSwipe(eventData.delta);
        }
    }
}
