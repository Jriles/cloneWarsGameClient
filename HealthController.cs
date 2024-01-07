using UnityEngine;
using UnityEngine.UI;
using GlobalHelpers;

public class HealthController : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;
    private Slider healthBar;
    [SerializeField]
    public AudioClip takeDmgSound1;
    [SerializeField]
    public AudioClip takeDmgSound2;
    private AudioSource audioSource;
    [SerializeField]
    public AudioClip deathSound1;
    private bool alive = true;
    private Animator animator;



    void Start()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth > 0) {
            animator.Play("HitReaction");
            PlayDamageSound();
            currentHealth -= damage;
            UpdateHealthBar();
        }


        if (currentHealth <= 0 && alive)
        {
            Die();
        }
    }

    private async void Die()
    {
        animator.Play("Dying");
        Destroy(GetComponent<Rigidbody>());
        Destroy(GetComponent<UnityEngine.AI.NavMeshAgent>());

        audioSource.volume = .3f;
        audioSource.PlayOneShot(deathSound1);
        alive = false;
        EnemyController enemyController = GetComponent<EnemyController>();
        if (enemyController != null) {
            enemyController.HandleDeath();
        }
        SinglePlayerController singlePlayerController = GetComponent<SinglePlayerController>();
        if (singlePlayerController != null) {
            singlePlayerController.HandleDeath();
        }
        await GlobalHelpersClass.WaitForSecondsTask(3000);

        Destroy(this.gameObject);
    }

    private void UpdateHealthBar()
    {
        // Check if the healthBar reference is assigned
        if (healthBar != null)
        {
            // Calculate the health percentage and update the UI Slider value
            float healthPercentage = (float)currentHealth / maxHealth;
            healthBar.value = healthPercentage;
        }
    }

    public void PlayDamageSound()
    {
        // Play the laser sound effect
        if (takeDmgSound1 != null && takeDmgSound2 != null)
        {
            audioSource.volume = 1f;
            audioSource.PlayOneShot(GetRandomClip());
        } else if(takeDmgSound1 != null) {
            audioSource.volume = 1f;
            audioSource.PlayOneShot(takeDmgSound1);
        }
    }

    private AudioClip GetRandomClip()
    {
        // Return a randomly chosen taking damage audio clip
        return (Random.Range(0, 2) == 0) ? takeDmgSound1 : takeDmgSound2;
    }

    public bool GetAlive () {
        return alive;
    }

    public void SetHealthBar (Slider healthBar)
    {
        this.healthBar = healthBar;
    }
}
