using UnityEngine;

public class XPDrop : MonoBehaviour
{
    //[SerializeField] private float xpValue = 5f;
    //[SerializeField] private float rotateSpeed = 90f;

    //[Header("Pickup Filter")]
    //[SerializeField] private LayerMask playerLayers; // set to Player layer in Inspector
    //[SerializeField] private string playerTag = "Player"; // fallback

    //private void Update()
    //{
    //   transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    //}

    //private void Reset()
    //{
    //    // Try to default to Player layer if it exists
    //    int layer = LayerMask.NameToLayer("Player");
    //    if (layer >= 0)
    //        playerLayers = 1 << layer;
    //}

    //private void OnTriggerEnter(Collider other)
    //{
    //    // Layer check
    //    bool isPlayerLayer = (playerLayers.value & (1 << other.gameObject.layer)) != 0;

    //    // Tag fallback (in case collider is on a child)
    //    bool isPlayerTag = other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag);

    //    if (!isPlayerLayer && !isPlayerTag)
    //        return;

    //    // Get BaseCharacter from collider, parent, or root
    //    BaseCharacter bc =
    //        other.GetComponent<BaseCharacter>() ??
    //        other.GetComponentInParent<BaseCharacter>() ??
    //        other.transform.root.GetComponent<BaseCharacter>();

    //    if (bc != null)
    //    {
    //        bc.GainXP(xpValue);
    //        Destroy(gameObject);
    //    }
    //}

    [SerializeField] private float xpValue = 5f;
    [SerializeField] private float rotateSpeed = 90f;

    private void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        BaseCharacter bc = other.GetComponentInParent<BaseCharacter>();
        if (bc != null)
        {
            bc.GainXP(xpValue);
            Destroy(gameObject);
        }
    }
}