using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatController : MonoBehaviour
{
    [SerializeField]
    public Transform muzzleTransform;
    [SerializeField]
    public GameObject laserPrefab;
    [SerializeField]
    public AudioClip laserSound;
    [SerializeField]
    public Transform rightHandTransform;
    [SerializeField]
    public Transform gunTransform;
    private Animator animator;


    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update() {
        if (rightHandTransform != null) {
            gunTransform.position = rightHandTransform.position;
            gunTransform.rotation = Quaternion.LookRotation(transform.forward);
        }
    }

    public void FireLaserProjectile(Quaternion direction)
    {
        animator.SetBool("IsFiring", true);
        GameObject laser = Instantiate(laserPrefab, muzzleTransform.position, direction);
        PlayLaserSound();
    }

    private void PlayLaserSound()
    {
        if (laserSound != null)
        {
            AudioSource.PlayClipAtPoint(laserSound, transform.position);
        }
    }
}
