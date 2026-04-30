using System.Collections.Generic;
using UnityEngine;

public class KnifeFanDaggerHitbox : MonoBehaviour
{
    private float damage;
    private LayerMask enemyLayers;
    private float hitCooldown;

    private readonly Dictionary<Transform, float> nextHitTimes = new();

    public void Init(float damage, LayerMask enemyLayers, float hitCooldown)
    {
        this.damage = damage;
        this.enemyLayers = enemyLayers;
        this.hitCooldown = hitCooldown;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayers.value) == 0)
            return;

        Transform root = other.transform.root;

        if (nextHitTimes.TryGetValue(root, out float nextTime))
        {
            if (Time.time < nextTime)
                return;
        }

        IDamageable dmg = other.GetComponentInParent<IDamageable>();

        if (dmg != null)
        {
            dmg.TakeDamage(damage);
            nextHitTimes[root] = Time.time + hitCooldown;
        }
    }
}