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

public class SinglePlayerController : MonoBehaviour
{
    private Camera camera;
    private Client client;
    private Rigidbody playerRigidBody;
    private int playerIdx;
    private PlayerState localPlayerState = PlayerState.Disabled;
    public AudioSource walkAudioSource;
    public AudioSource takeDamageSound;
    public AudioClip damageClip1;
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
    private int jumpThrust = 40;
    private float lastAttackTime = 0f;
    private float attackCooldown = .1f;
    private float lastWalkSendMsg = 0f;
    private float walkTimerLimit = .1f;
    private bool canJump = true;
    private bool alive = true;
    private SinglePlayerGameManager gameManager;
    public LayerMask shootableLayers;

    void Start ()
    {
      playerRigidBody = GetComponentInChildren<Rigidbody>();
      combatController = GetComponent<CombatController>();
      animator = GetComponent<Animator>();
      audioSource = GetComponent<AudioSource>();
      SetPlayerState(PlayerState.Idle);
    }

    // Update is called once per frame
    void Update()
    {
        if (localPlayerState != PlayerState.Disabled && alive)
        {
            bool isAnyWASDPressed = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);
            if (Input.GetKeyDown(KeyCode.Space) && canJump)
            {
                RenderJump();
                canJump = false;
            }
            
            if (!isAnyWASDPressed && !Input.GetMouseButton(0) && !Input.GetKeyDown(KeyCode.Space) && canJump) {
                RenderIdle();
            }

            if (Input.GetKey(KeyCode.LeftShift))
            {
                camera.fieldOfView = zoomedFOV;
            } else {
                camera.fieldOfView = defaultFOV;
            }
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            Vector3 movement = new Vector3(horizontal, 0f, vertical);

            if (isAnyWASDPressed )
            {
                RenderWalk(movement, cameraTransform.eulerAngles.y);
                RenderRotationChange(cameraTransform.eulerAngles.y);
                lastWalkSendMsg = Time.time;
            }

            if (CanAttack())
            {
                if (Input.GetMouseButton(0))
                {
                    RenderRotationChange(cameraTransform.eulerAngles.y);
                    RenderAttack(DetermineAttackPosition());
                    lastAttackTime = Time.time;
                } else if (animator.GetBool("IsFiring")) {
                    RenderStopAttacking();
                }
            }
        }
    }

    private Vector3 DetermineAttackPosition ()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // Create a RaycastHit variable to store information about the hit
        RaycastHit hit;

        // need a default target point in case we didnt hit anythign.
        Vector3 defaultTargetPoint = cameraTransform.position + (cameraTransform.forward * 50f);
        // Perform the raycast and check for a hit
        if (Physics.Raycast(ray, out hit, shootableLayers))
        {
            return hit.point;
        }
        return defaultTargetPoint;
    }

    bool CanAttack()
    {
        return Time.time - lastAttackTime >= attackCooldown;
    }

    bool CanWalk()
    {
        return Time.time - lastWalkSendMsg >= walkTimerLimit;
    }

    void OnCollisionEnter (Collision collision) {
        bool isTerrain = (terrainLayer.value & 1 << collision.gameObject.layer) != 0;
        bool localPlayerJumping = localPlayerState == PlayerState.Jumping;

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

    public void RenderTookDamage ()
    {
        audioSource.PlayOneShot(damageClip1);
        animator.Play("HitReaction");
    }

    //****************//
    //    Helpers     //
    //****************//
    public void SetCamera (Camera inputCamera)
    {
      camera = inputCamera;
    }

    public void SetMuzzlePoint (Transform muzzleTranformParam) 
    {
        muzzleTransform = muzzleTranformParam;
    }

    public void SetCameraTransform (Transform cameraTransformParam)
    {
        cameraTransform = cameraTransformParam;
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

    public void SetSinglePlayerGameManager (SinglePlayerGameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public void HandleDeath ()
    {
        gameManager.HandleDefeat();
    }
}
