using UnityEngine;

public class MagicOverheatProjectile : MonoBehaviour
{
    private float damage;
    private float currentSpeed;
    private float maxSpeed;
    private float acceleration;
    private LayerMask enemyLayers;

    public void Init(
        float damage,
        float startSpeed,
        float maxSpeed,
        float acceleration,
        float lifetime,
        LayerMask enemyLayers)
    {
        this.damage = damage;
        this.currentSpeed = startSpeed;
        this.maxSpeed = maxSpeed;
        this.acceleration = acceleration;
        this.enemyLayers = enemyLayers;

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        currentSpeed += acceleration * Time.deltaTime;
        currentSpeed = Mathf.Min(currentSpeed, maxSpeed);

        transform.position += transform.forward * currentSpeed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayers.value) == 0)
            return;

        IDamageable dmg = other.GetComponentInParent<IDamageable>();
        if (dmg != null)
            dmg.TakeDamage(damage);

        Destroy(gameObject);
    }
}