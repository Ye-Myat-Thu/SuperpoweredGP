using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class Movement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    [SerializeField] private CharacterCombat combat;
    [SerializeField] private GameObject moveIcon;
    [SerializeField] private CharacterVoice characterVoice;

    [Header("Movement Settings")]
    [SerializeField] private LayerMask clickableLayers;
    [SerializeField] public float lookRotationSpeed = 8f;

    [SerializeField] private TitanWhirlAbility whirlAbility;

    [Header("Hold to move")]
    [SerializeField] private bool rightClickRepeat = true;
    [SerializeField] private float repeatInterval = 0.05f;
    [SerializeField] private float maxRayDistance = 500f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] stepClips;
    [SerializeField] private float stepVolume = 1f;
    [SerializeField] private float stepPitch = 1f;
    [SerializeField] private float stepPitchRandom = 0.05f;

    private float nextRepeatTime;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        if (!characterVoice) characterVoice = GetComponent<CharacterVoice>();

        if (!audioSource) audioSource = GetComponent<AudioSource>();

        //if (!whirlAbility)
        //    whirlAbility = GetComponent<TitanWhirlAbility>();
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

        if (Input.GetKeyDown(KeyCode.S))
        {
            StopMovement();
        }

        FaceMovementDirection();
        UpdateAnimation();
    }

    private void StopMovement()
    {
        if (!agent) return;

        agent.ResetPath();

        agent.velocity = Vector3.zero;

        if (animator)
        {
            animator.ResetTrigger("Attack");
        }
    }

    //Old func
    //private void SetDestinationUnderMouse()
    //{
    //    Camera cam = Camera.main;
    //    if (!cam) return;

    //    Ray ray = cam.ScreenPointToRay(Input.mousePosition);
    //    if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, clickableLayers))
    //    {
    //        if (!agent.hasPath || Vector3.SqrMagnitude(agent.destination - hit.point) > 0.01f)
    //            agent.SetDestination(hit.point);

    //        Vector3 offset = new Vector3(hit.point.x, hit.point.y + 0.1f, hit.point.z);
    //        Instantiate(moveIcon, offset, Quaternion.identity);
    //    }
    //}

    private void SetDestinationUnderMouse()
    {
        Camera cam = Camera.main;
        if (!cam) return;

        characterVoice?.PlayMoveVoice();

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, clickableLayers))
        {
            bool changed = !agent.hasPath || Vector3.SqrMagnitude(agent.destination - hit.point) > 1f;

            if (changed)
            {
                agent.SetDestination(hit.point);

                Vector3 pos = hit.point + Vector3.up * 0.05f;

                Vector3 dir = hit.point - transform.position;
                dir.y = 0f;

                Quaternion rot = dir.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(dir.normalized, Vector3.up)
                    : Quaternion.identity;

                Instantiate(moveIcon, pos, rot);
            }
        }
    }

    void FaceMovementDirection()
    {
        //if (whirlAbility != null && whirlAbility.IsWhirling)
        //{
        //    return;
        //}

        //Vector3 v = agent.desiredVelocity;
        Vector3 v = agent.velocity;

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

    private int lastStepIndex = -1;

    private void PlayFootstep()
    {
        if (audioSource == null || stepClips == null || stepClips.Length == 0)
            return;

        int index;

        do
        {
            index = UnityEngine.Random.Range(0, stepClips.Length);
        }
        while (index == lastStepIndex && stepClips.Length > 1);

        lastStepIndex = index;

        float pitch = stepPitch + UnityEngine.Random.Range(-stepPitchRandom, stepPitchRandom);

        audioSource.pitch = pitch;
        audioSource.PlayOneShot(stepClips[index], stepVolume);
    }
}
