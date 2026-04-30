using UnityEngine;

public class XPDrop : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private BaseCharacter targetCharacter;
    
    [Header("XP Settings")]
    [SerializeField] private float rotateSpeed = 90f;
    [SerializeField] private int xpLevel1To6 = 25;
    [SerializeField] private int xpLevel7To12 = 20;
    [SerializeField] private int xpLevel13To20 = 15;
    [SerializeField] private int minimumXpValue = 1;

    private void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        BaseCharacter bc = other.GetComponentInChildren<BaseCharacter>() ?? other.GetComponentInParent<BaseCharacter>() ?? other.transform.root.GetComponent<BaseCharacter>();
        if (bc == null)
            return;

        int xpValue = GetXPValueForLevel(bc.Level);
        bc.GainXP(xpValue);

        Destroy(gameObject);
    }

    private int GetXPValueForLevel(int playerLevel)
    {
        if (playerLevel <= 6)
            return xpLevel1To6;

        if (playerLevel <= 12)
            return xpLevel7To12;

        if (playerLevel <= 20)
            return xpLevel13To20;

        int reduceValue = xpLevel13To20 - (playerLevel - 20);
        return Mathf.Max(minimumXpValue, reduceValue);
    }

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


}