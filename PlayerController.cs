using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GlobalData;
using System.Linq;
using GlobalHelpers;
#if UNITY_EDITOR
using ParrelSync;
#endif

public class PlayerController : MonoBehaviour
{
    private Camera camera;
    private Client client;
    private Rigidbody playerRigidBody;
    private int playerIdx;
    GameManager manager;
    private bool isLocalClient = false;
    private Slider healthBar;
    private PlayerState localPlayerState = PlayerState.Disabled;
    private Transform cameraTransform;
    private Transform muzzleTransform;
    [SerializeField]
    public GameObject laserPrefab;
    private CombatController combatController;
    private Animator animator;
    public float defaultFOV = 60.0f;
    public float zoomedFOV = 30.0f;
    [SerializeField]
    public LayerMask terrainLayer;
    private AudioSource audioSource;
    public AudioClip damageClip1;
    private int jumpThrust = 40;

    private float lastAttackTime = 0f;
    private float attackCooldown = .1f;
    private float lastWalkSendMsg = 0f;
    private float walkTimerLimit = .1f;
    private bool canJump = true;
    private bool alive = true;

    public LayerMask shootableLayers;

    void Start ()
    {
      playerRigidBody = GetComponentInChildren<Rigidbody>();
      combatController = GetComponent<CombatController>();
      animator = GetComponent<Animator>();
      audioSource = GetComponent<AudioSource>();
      healthBar.value = 1;
      if (isLocalClient){
        //InvokeRepeating("LocalPlayerPositionSync", 2.0f, 1.0f);
      }
    }

    // Update is called once per frame
    void Update()
    {
        if (isLocalClient && localPlayerState != PlayerState.Disabled && alive)
        {
            bool isAnyWASDPressed = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);
            if (Input.GetKeyDown(KeyCode.Space) && canJump)
            {
                SendJump();
                canJump = false;
            }
            
            if (!isAnyWASDPressed && !Input.GetMouseButton(0) && !Input.GetKeyDown(KeyCode.Space) && canJump) {
                SendIdle();
            }

            if (Input.GetKey(KeyCode.LeftShift))
            {
                camera.fieldOfView = zoomedFOV;
            } else {
                camera.fieldOfView = defaultFOV;
            }
        }
    }
    void FixedUpdate ()
    {
        if (isLocalClient && localPlayerState != PlayerState.Disabled && alive)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            Vector3 movement = new Vector3(horizontal, 0f, vertical);

            bool isAnyWASDPressed = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);
            if (isAnyWASDPressed )
            {
                SendWalk(movement, cameraTransform.eulerAngles.y);
                lastWalkSendMsg = Time.time;
            }
            bool wasAnyWASDPressed = Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.D);
            if (wasAnyWASDPressed && localPlayerState == PlayerState.Walking)
            {
                SendPosition(transform.position, cameraTransform.eulerAngles.y);
            }

            if (CanAttack())
            {
                if (Input.GetMouseButton(0))
                {
                    SendRotationChange(cameraTransform.eulerAngles.y);
                    SendAttack();
                    lastAttackTime = Time.time;
                } else if (animator.GetBool("IsFiring")) {
                    SendStopAttacking();
                }
            }
        }
    }

    bool CanAttack()
    {
        return Time.time - lastAttackTime >= attackCooldown;
    }

    bool CanWalk()
    {
        return Time.time - lastWalkSendMsg >= walkTimerLimit;
    }

    //****************//
    //Server Messages //
    //****************//
    public void LocalPlayerPositionSync ()
    {
        SendPosition(transform.position, cameraTransform.eulerAngles.y);
    }

    public void SendIdle ()
    {
      if (isLocalClient)
      {
        JObject idleMsg = new JObject(
          new JProperty(GlobalConstants.GameMsgOpCodeKey, 8),
          new JProperty(GlobalConstants.GameMsgPlayerIdxKey, playerIdx)
        );
        client.SendMessageToServer(idleMsg, client.gameClient);
      }
    }

    public void SendRotationChange (float newPlayerYRotation) {
        JObject newRotationMessage = new JObject(
            new JProperty(GlobalConstants.GameMsgOpCodeKey, 4),
            new JProperty(GlobalConstants.GameMsgPlayerIdxKey, playerIdx),
            new JProperty(GlobalConstants.GameMsgContentKey, newPlayerYRotation)
        );
        client.SendMessageToServer(newRotationMessage, client.gameClient);
    }

    public void SendWalk (Vector3 movementDirection, float playerYRotation)
    {
        JObject walkMessage = new JObject(
            new JProperty(GlobalConstants.GameMsgOpCodeKey, 2),
            new JProperty(GlobalConstants.GameMsgPlayerIdxKey, playerIdx),
            new JProperty(
                GlobalConstants.GameMsgContentKey, 
                new JObject(
                    new JProperty(
                        GlobalConstants.WalkDirKey,
                        new JObject(
                            new JProperty(GlobalConstants.XVector3Key, movementDirection.x),
                            new JProperty(GlobalConstants.YVector3Key, movementDirection.y),
                            new JProperty(GlobalConstants.ZVector3Key, movementDirection.z)
                        )
                    ),
                    new JProperty(GlobalConstants.PlayerYRotationKey, playerYRotation)
                )
            )
        );
        client.SendMessageToServer(walkMessage, client.gameClient);
    }

    public void SendPosition (Vector3 newPosition, float playerYRotation)
    {
        JObject positionMsg = new JObject(
            new JProperty(GlobalConstants.GameMsgOpCodeKey, 9),
            new JProperty(GlobalConstants.GameMsgPlayerIdxKey, playerIdx),
            new JProperty(
                GlobalConstants.GameMsgContentKey,
                new JObject(
                    new JProperty(
                        GlobalConstants.NewPosKey,
                        new JObject(
                            new JProperty(GlobalConstants.XVector3Key, newPosition.x),
                            new JProperty(GlobalConstants.YVector3Key, newPosition.y),
                            new JProperty(GlobalConstants.ZVector3Key, newPosition.z)
                        )
                    ),
                    new JProperty(GlobalConstants.PlayerYRotationKey, playerYRotation)
                )
            )
        );
        client.SendMessageToServer(positionMsg, client.gameClient);
    }

    public void SendAttack ()
    {
      if (isLocalClient)
      {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // Create a RaycastHit variable to store information about the hit
        RaycastHit hit;

        // need a default target point in case we didnt hit anythign.
        Vector3 defaultTargetPoint = cameraTransform.position + (cameraTransform.forward * 50f);
        JObject targetPointObj = new JObject(
            new JProperty(GlobalConstants.XVector3Key, defaultTargetPoint.x),
            new JProperty(GlobalConstants.YVector3Key, defaultTargetPoint.y),
            new JProperty(GlobalConstants.ZVector3Key, defaultTargetPoint.z)
        );
        // Perform the raycast and check for a hit
        if (Physics.Raycast(ray, out hit, shootableLayers))
        {
            targetPointObj = new JObject(
                new JProperty(GlobalConstants.XVector3Key, hit.point.x),
                new JProperty(GlobalConstants.YVector3Key, hit.point.y),
                new JProperty(GlobalConstants.ZVector3Key, hit.point.z)
            );
        }

        JObject attackMsg = new JObject(
            new JProperty(GlobalConstants.GameMsgOpCodeKey, 5),
            new JProperty(GlobalConstants.GameMsgPlayerIdxKey, playerIdx),
            new JProperty(GlobalConstants.GameMsgContentKey, targetPointObj)
        );
        client.SendMessageToServer(attackMsg, client.gameClient);
      }
    }

    public void SendJump ()
    {
      if (isLocalClient){
        JObject jumpMsg = new JObject(
          new JProperty(GlobalConstants.GameMsgOpCodeKey, 1),
          new JProperty(GlobalConstants.GameMsgPlayerIdxKey, playerIdx)
        );
        client.SendMessageToServer(jumpMsg, client.gameClient);
      }
    }

    //only call me if im local client
    public void SendTookDamage (int damageAmount)
    {
      JObject damageMsg = new JObject(
        new JProperty(GlobalConstants.GameMsgOpCodeKey, 6),
        new JProperty(GlobalConstants.GameMsgPlayerIdxKey, playerIdx),
        new JProperty(GlobalConstants.GameMsgContentKey, damageAmount)
      );
      client.SendMessageToServer(damageMsg, client.gameClient);
    }

    public void SendStopAttacking ()
    {
        JObject stopAttackingMsg = new JObject(
            new JProperty(GlobalConstants.GameMsgOpCodeKey, 10),
            new JProperty(GlobalConstants.GameMsgPlayerIdxKey, playerIdx)
        );
        client.SendMessageToServer(stopAttackingMsg, client.gameClient);
    }

    void OnCollisionEnter (Collision collision) {
        bool isTerrain = (terrainLayer.value & 1 << collision.gameObject.layer) != 0;
        bool localPlayerJumping = localPlayerState == PlayerState.Jumping;

        if (isTerrain && localPlayerJumping && isLocalClient)
        {
            SendPosition(transform.position, cameraTransform.eulerAngles.y);
        }

        if (isTerrain) 
        {
            animator.SetBool("IsJumping", false);
            canJump = true;
        }
    }
    //****************//
    // Render Methods //
    //****************//
    public void RenderStopAttacking ()
    {
        animator.SetBool("IsFiring", false);
    }
    public void RenderNewPosition (Vector3 newPosition) 
    {
        playerRigidBody.MovePosition(newPosition);
    }
    public void RenderAttack (Vector3 cameraForwardHitPosition)
    {
        animator.SetBool("IsWalking", false);
        Vector3 direction = (cameraForwardHitPosition - muzzleTransform.position).normalized;
        combatController.FireLaserProjectile(Quaternion.LookRotation(direction));
    }

    public void RenderRotationChange (float newPlayerYRotation)
    {
        transform.eulerAngles = new Vector3(0, newPlayerYRotation, 0);
    }

    public void RenderWalk (Vector3 movementDirection, float newPlayerYRotation)
    {
        animator.SetBool("IsWalking", true);
        
        Quaternion newRotation = Quaternion.Euler(0, newPlayerYRotation, 0);
        playerRigidBody.MoveRotation(newRotation);
        playerRigidBody.MovePosition(
            playerRigidBody.position + transform.TransformDirection(movementDirection.normalized) * GlobalConstants.PlayerSpeed * Time.fixedDeltaTime
        );
    }

    public void RenderIdle ()
    {
        animator.SetBool("IsJumping", false);
        animator.SetBool("IsFiring", false);
        animator.SetBool("IsWalking", false);
        animator.SetTrigger("Idle");
    }

    public void RenderJump ()
    {
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsJumping", true);
        playerRigidBody.AddForce(Vector3.up * jumpThrust, ForceMode.Impulse);
    }
    public void RenderNewHealth (float newHealthPercent)
    {
        healthBar.value = newHealthPercent;
    }

    public void RenderTookDamage ()
    {
        audioSource.PlayOneShot(damageClip1);
        animator.Play("HitReaction");
    }

    public void Die()
    {
        alive = false;
        animator.Play("Dying");
    }

    //****************//
    //    Helpers     //
    //****************//
    public void SetCamera (Camera inputCamera)
    {
      camera = inputCamera;
    }

    public void SetClient (Client inputClient)
    {
      client = inputClient;
    }

    public int GetPlayerIdx ()
    {
      return playerIdx;
    }

    public void SetPlayerIdx (int inputIdx)
    {
      playerIdx = inputIdx;
    }

    public void SetGameManager (GameManager inputManager)
    {
      manager = inputManager;
    }

    public void SetMuzzlePoint (Transform muzzleTranformParam) 
    {
        muzzleTransform = muzzleTranformParam;
    }

    public void TurnOnCamera()
    {
      GetComponentInChildren<Camera>().enabled = true;
    }

    public void TurnOnAudioListener()
    {
      GetComponentInChildren<AudioListener>().enabled = true;
    }

    public void SetIsLocalClient ()
    {
      isLocalClient = true;
    }

    public void SetCameraTransform (Transform cameraTransformParam)
    {
        cameraTransform = cameraTransformParam;
    }

    public bool GetIsLocalClient ()
    {
      return isLocalClient;
    }

    public void SetHealthBar (Slider inputHealthBar)
    {
        healthBar = inputHealthBar;
    }

    public void SetPlayerState (PlayerState inputState)
    {
      localPlayerState = inputState;
    }

    public PlayerState GetPlayerState ()
    {
      return localPlayerState;
    }

    public void SetAlive (bool aliveParam)
    {
        alive = aliveParam;
    }

    public bool GetAlive ()
    {
        return alive;
    }
}
