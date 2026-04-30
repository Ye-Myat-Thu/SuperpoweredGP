using System.Collections.Generic;
using UnityEngine;

public class OrbitingSword : MonoBehaviour
{
    private Transform owner;
    private float orbitRadius;
    private float orbitSpeed;
    private float selfSpinSpeed;
    private float currentAngle;
    private float damage;
    private LayerMask enemyMask;
    private Vector3 rotationOffset;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitClip;

    private readonly HashSet<IDamageable> damagedThisPass = new();

    public void Initialize(
        Transform owner,
        float orbitRadius,
        float orbitSpeed,
        float selfSpinSpeed,
        float startAngle,
        float damage,
        LayerMask enemyMask,
        Vector3 rotationOffset)
    {
        this.owner = owner;
        this.orbitRadius = orbitRadius;
        this.orbitSpeed = orbitSpeed;
        this.selfSpinSpeed = selfSpinSpeed;
        this.currentAngle = startAngle;
        this.damage = damage;
        this.enemyMask = enemyMask;
        this.rotationOffset = rotationOffset;
    }

    private void Awake()
    {
        if (!audioSource)
            audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (!owner)
        {
            Destroy(gameObject);
            return;
        }

        currentAngle += orbitSpeed * Time.deltaTime;

        float rad = currentAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * orbitRadius;

        transform.position = owner.position + offset;

        Vector3 tangent = new Vector3(-Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        if (tangent.sqrMagnitude > 0.001f)
        {
            Quaternion orbitRotation = Quaternion.LookRotation(tangent.normalized, Vector3.up);
            Quaternion spinRotation = Quaternion.Euler(0f, selfSpinSpeed * Time.time, 0f);
            transform.rotation = orbitRotation * spinRotation * Quaternion.Euler(rotationOffset);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyMask.value) == 0)
            return;

        IDamageable dmg = other.GetComponentInParent<IDamageable>();
        if (dmg == null) return;

        if (damagedThisPass.Contains(dmg)) return;

        if (audioSource && hitClip != null)
            audioSource.PlayOneShot(hitClip);

        damagedThisPass.Add(dmg);
        dmg.TakeDamage(damage);
    }

    private void OnTriggerExit(Collider other)
    {
        IDamageable dmg = other.GetComponentInParent<IDamageable>();
        if (dmg != null)
            damagedThisPass.Remove(dmg);
    }
}