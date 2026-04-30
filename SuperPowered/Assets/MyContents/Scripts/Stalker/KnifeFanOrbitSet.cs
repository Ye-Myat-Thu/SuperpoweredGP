using UnityEngine;

public class KnifeFanOrbitSet : MonoBehaviour
{
    private Transform orbitCenter;
    private float rotationSpeed;
    private float duration;
    private float elapsed;

    public void Init(Transform orbitCenter, float rotationSpeed, float duration)
    {
        this.orbitCenter = orbitCenter;
        this.rotationSpeed = rotationSpeed;
        this.duration = duration;
    }

    private void Update()
    {
        if (!orbitCenter) return;

        elapsed += Time.deltaTime;

        transform.position = orbitCenter.position;
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        if (elapsed >= duration)
            Destroy(gameObject);
    }
}