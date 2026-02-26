using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class MoveIcon : MonoBehaviour
{
    [SerializeField] private GameObject moveIcon;
    private Vector3 scaleChange;

    private void Awake()
    {
        scaleChange = new Vector3(-0.01f, -0.01f, -0.01f);
    }

    private void Update()
    {
        moveIcon.transform.localScale += scaleChange;

        if (moveIcon.transform.localScale.y < 0.1f || moveIcon.transform.localScale.y > 0.1f )
        {
            scaleChange = -scaleChange;
        }
    }
}
