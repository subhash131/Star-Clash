using System;
using System.IO;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;


public class MyPlayer: MonoBehaviour, IDragHandler, IPunObservable{

    [Header("References")]
    private InputActions inputActions;
    [SerializeField]private Rigidbody playerRigidbody;
    private FixedJoystick joystick;
    public Camera fppCamera;
    public Transform firePoint; 
    private float lastFireTime = 0f;
    private PhotonView view;

    [Header("Shooting")]
    [SerializeField] private float shootForce = 100f;
    [SerializeField] private float fireCoolDown = 0.2f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;

    [Header("Rotation")]
    [SerializeField] private float rotationPerSwipe = 90f;
    [SerializeField] private float rotationSmoothSpeed = 10f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Camera Lag")]
    [SerializeField] private float cameraLagFactor = 0.2f;

    private Quaternion targetRotation;
    private Vector3 cameraInitialLocalPosition;
    private Vector3 cameraVelocity;
    private float yaw;   // left/right
    private float pitch; // up/down

    private void Awake(){
        playerRigidbody = GetComponent<Rigidbody>();
        inputActions = new InputActions();
        view = GetComponent<PhotonView>();
        cameraInitialLocalPosition = fppCamera.transform.localPosition;
        // Ensure the camera is disabled for non-local players
        if (!view.IsMine) {
            fppCamera.enabled = false;
            fppCamera.gameObject.SetActive(false);
            inputActions.Disable(); 
        }
    }

    void Start(){
        if(view.IsMine){
            MenuManager.instance.menuCamera.SetActive(false);
            joystick = CanvasManager.instance.joystick;
            SwipeInput.instance.player = this;
            ButtonShooter.instance.player = this;
        }
    }

    private void Update()
    {
        if (!view.IsMine) return; 

        // Smoothly rotate player
        if (playerRigidbody.rotation != targetRotation) {
            Quaternion newRotation = Quaternion.Slerp(
                            playerRigidbody.rotation,
                            targetRotation,
                            rotationSmoothSpeed * Time.deltaTime
                            ).normalized;
            playerRigidbody.MoveRotation(newRotation);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info){
        if (stream.IsWriting)
        {
            // We own this player: send position, rotation, and velocity to others
            stream.SendNext(playerRigidbody.position);
            stream.SendNext(playerRigidbody.rotation);
            stream.SendNext(playerRigidbody.velocity);
        }
        else
        {
            // Network player: receive data
            playerRigidbody.position = (Vector3)stream.ReceiveNext();
            playerRigidbody.rotation = (Quaternion)stream.ReceiveNext();
            playerRigidbody.velocity = (Vector3)stream.ReceiveNext();
        }
    }

    private void FixedUpdate(){
        if (!view.IsMine) return; 

        Vector3 input = new Vector3(joystick.Horizontal, 0f, joystick.Vertical).normalized;
        Vector3 worldDirection = transform.TransformDirection(input) * moveSpeed;
        worldDirection.y = playerRigidbody.velocity.y; // Keep current vertical velocity
        playerRigidbody.velocity = worldDirection;

        Vector3 lagOffset = new(
            -joystick.Horizontal * moveSpeed * cameraLagFactor,
            0f,
            -joystick.Vertical * moveSpeed * cameraLagFactor
        );
        Vector3 targetCameraLocalPos = cameraInitialLocalPosition + lagOffset;
        fppCamera.transform.localPosition = Vector3.SmoothDamp(
            fppCamera.transform.localPosition,
            targetCameraLocalPos,
            ref cameraVelocity,
            0.1f
        );        
        PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Score", out object currentScore);
        if(RoomManager.instance.scoreSlider != null && currentScore != null)
            RoomManager.instance.scoreSlider.value = float.Parse(currentScore.ToString());
        if(RoomManager.instance.myScoreText != null && currentScore != null){
            RoomManager.instance.myScoreText.text = currentScore.ToString();
            if(int.Parse(currentScore.ToString()) <= 0){
                // Player has no score left, handle accordingly
                Debug.Log($"Player {PhotonNetwork.LocalPlayer.NickName} has reached zero score.");
                // PhotonNetwork.Destroy(view.gameObject); // Destroy the player object
                RoomManager.instance.gameOverPanel.SetActive(true); // Show game over panel
                RoomManager.instance.resultText.text = $"You lose!";
            }
        }
    }

    public void Shoot(){
        if (view == null) {
            // Debug.LogWarning("Shoot aborted: view is null or not mine!");
            return;
        }
        if (Time.time - lastFireTime < fireCoolDown) return;
        GameObject projectile = PhotonNetwork.Instantiate(
            Path.Combine("PhotonPrefabs", "Projectile"),
            firePoint.position,
            firePoint.rotation
        );
        projectile.GetComponent<Projectile>().ownerActorNumber = view.OwnerActorNr; 

        Ray ray = fppCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 direction = ray.direction.normalized;

        if (projectile.TryGetComponent(out Rigidbody rb)){
            rb.velocity = direction * shootForce;
        }

        lastFireTime = Time.time;
    }

    public void OnSwipe(Vector2 delta) {
        if (!view.IsMine) return; 

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        float horizontalPercent = delta.x / screenWidth;
        float verticalPercent = delta.y / screenHeight;

        yaw += horizontalPercent * rotationPerSwipe;
        pitch -= verticalPercent * rotationPerSwipe;

        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
        targetRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    public void OnDrag(PointerEventData eventData){
        if (!view.IsMine) return; 

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        float horizontalPercent = eventData.delta.x / screenWidth;
        float verticalPercent = eventData.delta.y / screenHeight;

        yaw += horizontalPercent * rotationPerSwipe;
        pitch -= verticalPercent * rotationPerSwipe;

        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
        targetRotation = Quaternion.Euler(pitch, yaw, 0f);
    }


 


}