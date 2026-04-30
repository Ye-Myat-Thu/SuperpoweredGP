using UnityEngine;

public class BossWaveProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float damage;

    [SerializeField] private ParticleSystem splashPrefab;
    [SerializeField] private Vector3 vfxOffset = Vector3.zero;
    [SerializeField] private bool useSurfaceNormal = true;

    public void Initialize(Vector3 newDirection, float newSpeed, float newDamage, float lifeTime)
    {
        direction = newDirection.normalized;
        speed = newSpeed;
        damage = newDamage;

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        direction.y = 0f;
        transform.position += direction.normalized * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        IDamageable dmg = other.GetComponentInParent<IDamageable>();

        if (dmg != null)
        {
            dmg.TakeDamage(damage);
            Destroy(gameObject);
        }

        SpawnHitVfx(other);
        Destroy(gameObject);
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
