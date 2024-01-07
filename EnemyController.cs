using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    private int health = 100;
    [SerializeField]
    private Transform target;
    private NavMeshAgent navMeshAgent;
    [SerializeField]
    public float engagementDistance = 40f;
    [SerializeField]
    public GameObject laserPrefab;
    public float laserfireRate = 1f;
    private float timeSinceLastShot = 0f;
    [SerializeField]
    public Transform muzzleTransform;
    private CombatController combatController;
    private HealthController healthController;
    private Animator animator;
    private SinglePlayerGameManager gameManager;

    void Start()
    {
        // Get the NavMeshAgent component
        navMeshAgent = GetComponent<NavMeshAgent>();
        combatController = GetComponent<CombatController>();
        healthController = GetComponent<HealthController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (healthController.GetAlive()) {
            timeSinceLastShot += Time.deltaTime;
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            RenderAttackMode(distanceToTarget);

            animator.SetBool("IsFiring", false);
            animator.SetBool("IsWalking", true);
            
            Vector3 modifiedTargetPosition = target.position + Vector3.up;

            if (timeSinceLastShot >= 1f / laserfireRate && CanAttack(modifiedTargetPosition, distanceToTarget)) {
                animator.SetBool("IsWalking", false);

                // Calculate the direction from the firing object to the modified target position
                Vector3 directionToModifiedTarget = (modifiedTargetPosition - muzzleTransform.position).normalized;

                // Calculate the rotation needed to point the laser at the player
                Quaternion rotationToPlayer = Quaternion.LookRotation(directionToModifiedTarget);
                transform.rotation = rotationToPlayer;
                combatController.FireLaserProjectile(rotationToPlayer);
                timeSinceLastShot = 0f;
            }
        }
    }

    bool CanAttack (Vector3 targetPos, float distanceToTarget)
    {
        Vector3 attackDir = (targetPos - muzzleTransform.position).normalized;
        Ray ray = new Ray(muzzleTransform.position, attackDir);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform == target && distanceToTarget < engagementDistance)
            {
                return true;
            }
        }
        return false;
    }

    public void SetTarget (Transform target)
    {
        this.target = target;
    }

    void RenderAttackMode(float distanceToTarget)
    {
        if (distanceToTarget > engagementDistance) {
            navMeshAgent.SetDestination(target.position);
        } else {
            navMeshAgent.ResetPath();
        }
    }
    
    public void SetSinglePlayerGameManager (SinglePlayerGameManager singlePlayerGameManager)
    {
        this.gameManager = singlePlayerGameManager;
    }

    public void HandleDeath ()
    {
        gameManager.DecrementCurrentWaveEnemyCount();
    }
}