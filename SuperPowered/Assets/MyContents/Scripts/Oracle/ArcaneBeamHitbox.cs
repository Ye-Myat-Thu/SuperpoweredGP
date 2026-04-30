using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcaneBeamHitbox : MonoBehaviour
{
    private float dps;
    private LayerMask enemyLayers;

    [SerializeField] private float tickInterval = 0.1f;

    private readonly Dictionary<Transform, Coroutine> activeTargets = new();

    public void Init(float dps, LayerMask enemyLayers)
    {
        this.dps = dps;
        this.enemyLayers = enemyLayers;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryStartDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryStartDamage(other);
    }

    private void OnTriggerExit(Collider other)
    {
        StopDamage(other);
    }

    private void TryStartDamage(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayers.value) == 0)
            return;

        IDamageable dmg = other.GetComponentInParent<IDamageable>();
        if (dmg == null) return;

        Transform root = other.transform.root;

        if (activeTargets.ContainsKey(root))
            return;

        Coroutine routine = StartCoroutine(DamageRoutine(root, dmg));
        activeTargets.Add(root, routine);
    }

    private void StopDamage(Collider other)
    {
        Transform root = other.transform.root;

        if (!activeTargets.TryGetValue(root, out Coroutine routine))
            return;

        if (routine != null)
            StopCoroutine(routine);

        activeTargets.Remove(root);
    }

    private IEnumerator DamageRoutine(Transform targetRoot, IDamageable dmg)
    {
        while (targetRoot != null)
        {
            float damageThisTick = dps * tickInterval;
            dmg.TakeDamage(damageThisTick);

            yield return new WaitForSeconds(tickInterval);
        }
    }

    private void OnDisable()
    {
        foreach (var pair in activeTargets)
        {
            if (pair.Value != null)
                StopCoroutine(pair.Value);
        }

        activeTargets.Clear();
    }
}