using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserController : MonoBehaviour
{
    public int damage = 20;
    private int speed = 70;
    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // composition over inheritance
        // multiplayer
        PlayerController controller = other.GetComponent<PlayerController>();
        if (controller != null && controller.GetIsLocalClient() && controller.GetAlive())
        {
            controller.SendTookDamage(damage);
        }

        // single player
        HealthController enemyController = other.GetComponent<HealthController>();
        if (enemyController != null)
        {
            enemyController.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
