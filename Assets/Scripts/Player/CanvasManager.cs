using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasManager : MonoBehaviour{

    public static CanvasManager instance;
    public FixedJoystick joystick;

    void Awake(){
        instance = this;
    }

}
