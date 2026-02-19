using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float damage;
    private float speed;
    private float lifetime;
    private LayerMask hitMask;

    private float dieTime;

    public void Init(float damage, float speed, float lifetime, LayerMask hitMask)
    {
        this.damage = damage;
        this.speed = speed;
        this.lifetime = lifetime;
        this.hitMask = hitMask;

        dieTime = Time.time + lifetime;
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;

        if (Time.time > dieTime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & hitMask) == 0)
        {
            return;
        }
        var dmg = other.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
