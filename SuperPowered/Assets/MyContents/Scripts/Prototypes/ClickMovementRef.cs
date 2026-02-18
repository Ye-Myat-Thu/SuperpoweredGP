using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class ClickMovementRef : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    [Header("Movement Settings")]
    [SerializeField] private LayerMask clickableLayers;
    [SerializeField] private float lookRotationSpeed = 8f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        //Right Click to Set Destination
        if (Input.GetMouseButtonDown(1)) 
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, clickableLayers))
            {
                agent.SetDestination(hit.point);
            }
        }

        //Face the direction of travel smoothly
        FaceMovementDirection();

        //Update Animations
        //UpdateAnimation();
    }

    void FaceMovementDirection()
    {
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * lookRotationSpeed);
        }
    }

    void UpdateAnimation()
    {
        if (animator != null)
        {
            //Calculate speed based on agent velocity
            float currentSpeed = agent.velocity.magnitude / agent.speed;
            animator.SetFloat("Speed", currentSpeed, 0.1f, Time.deltaTime);
        }
    }
}
