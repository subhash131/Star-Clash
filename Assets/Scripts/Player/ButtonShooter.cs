using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class ButtonShooter : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler{

    public static ButtonShooter instance;
    public MyPlayer player;
    public float holdFireRate = 0.2f;

    private bool isHolding = false;
    private Coroutine fireCoroutine;

    void Awake(){
        instance = this;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (player == null) return;
        player.Shoot(); // Fire once on tap
        isHolding = true;
        fireCoroutine = StartCoroutine(AutoFire());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
        if (fireCoroutine != null)
            StopCoroutine(fireCoroutine);
    }

    private IEnumerator AutoFire(){
        yield return new WaitForSeconds(holdFireRate); 
        while (isHolding)
        {
            player.Shoot();
            yield return new WaitForSeconds(holdFireRate);
        }
    }

    public void OnDrag(PointerEventData eventData){
        if (player != null) {
            player.OnSwipe(eventData.delta);
        }
    }
}
