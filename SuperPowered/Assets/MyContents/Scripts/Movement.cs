using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class Movement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    [Header("Movement Settings")]
    [SerializeField] private LayerMask clickableLayers;
    [SerializeField] public float lookRotationSpeed = 8f;

    [Header("Hold to move")]
    [SerializeField] private bool rightClickRepeat = true;
    [SerializeField] private float repeatInterval = 0.05f;
    [SerializeField] private float maxRayDistance = 500f;

    private float nextRepeatTime;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (rightClickRepeat)
        {
            if (Input.GetMouseButton(1) && Time.time >= nextRepeatTime)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                nextRepeatTime = Time.time + repeatInterval;
                SetDestinationUnderMouse();
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(1))
            {
                SetDestinationUnderMouse();
            }
        }

        FaceMovementDirection();
        UpdateAnimation();
    }

    private void SetDestinationUnderMouse()
    {
        Camera cam = Camera.main;
        if (!cam) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, clickableLayers))
        {
            if (!agent.hasPath || Vector3.SqrMagnitude(agent.destination - hit.point) > 0.01f)
                agent.SetDestination(hit.point);
        }
    }

    void FaceMovementDirection()
    {
        Vector3 v = agent.desiredVelocity;

        if (v.sqrMagnitude > 0.1f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(v.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * lookRotationSpeed);
        }
    }

    void UpdateAnimation()
    {
        if (animator != null && agent != null)
        {
            float currentSpeed = agent.velocity.magnitude / Mathf.Max(agent.speed, 0.001f);
            animator.SetFloat("Speed", currentSpeed, 0.1f, Time.deltaTime);
        }
    }
}
