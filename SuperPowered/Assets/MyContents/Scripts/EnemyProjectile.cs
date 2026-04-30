using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("VFX")]
    [SerializeField] private ParticleSystem splashPrefab;
    [SerializeField] private Vector3 vfxOffset = Vector3.zero;
    [SerializeField] private bool useSurfaceNormal = true;

    [SerializeField] private float lifeTime = 5f;

    private Transform target;
    private float speed;
    private int damage;
    private Vector3 moveDirection;

    public void Initialize(Transform newTarget, int newDamage, float newSpeed)
    {
        target = newTarget;
        damage = newDamage;
        speed = newSpeed;

        if (target != null)
        {
            moveDirection = (target.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (target == null) return;

        if (other.transform == target || other.transform.IsChildOf(target))
        {
            // Apply damage
            IDamageable dmg = other.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(damage);
            }

            // Spawn hit VFX
            SpawnHitVfx(other);

            Destroy(gameObject);
        }
    }

    private void SpawnHitVfx(Collider hit)
    {
        if (splashPrefab == null) return;

        Vector3 spawnPos = transform.position + vfxOffset;
        Quaternion rot = Quaternion.identity;

        if (useSurfaceNormal)
        {
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hitInfo, 2f))
            {
                spawnPos = hitInfo.point + vfxOffset;
                rot = Quaternion.LookRotation(hitInfo.normal);
            }
        }

        ParticleSystem fx = Instantiate(splashPrefab, spawnPos, rot);
        Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax);
    }
}