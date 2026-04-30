using UnityEngine;

public class FastProjectile : MonoBehaviour
{
    private float damage;
    private float speed;
    private float lifetime;
    private LayerMask enemyLayers;

    public void Init(float damage, float speed, float lifetime, LayerMask enemyLayers)
    {
        this.damage = damage;
        this.speed = speed;
        this.lifetime = lifetime;
        this.enemyLayers = enemyLayers;

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
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