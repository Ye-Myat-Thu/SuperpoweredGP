using System.Runtime.CompilerServices;
using UnityEngine;

public class ObjectDetector : MonoBehaviour
{
    public Transform cameraTransform;
    public LayerMask obstructionMask;

    private FadingObject _currentObject;

    private void LaeUpdate()
    {
        if (!cameraTransform) return;

        Vector3 origin = cameraTransform.position;
        Vector3 target = transform.position;
        Vector3 dir = target - origin;
        float dist = dir.magnitude;

        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dist, obstructionMask))
        {
            Debug.Log("Hit an object");
            FadingObject fade = hit.collider.GetComponent<FadingObject>();

            if (fade != null && fade != _currentObject)
            {
                if (_currentObject != null)
                    _currentObject.FadeIn();

                fade.FadeOut();
                _currentObject = fade;
            }
        }
        else
        {
            if (_currentObject != null)
            {
                _currentObject.FadeIn();
                _currentObject = null;
            }
        }

    }
}
