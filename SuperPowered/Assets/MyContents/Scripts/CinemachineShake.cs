using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class CinemachineShake : MonoBehaviour
{
    private CinemachineBasicMultiChannelPerlin noise;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        noise = GetComponent<CinemachineBasicMultiChannelPerlin>();
    }

    public void Shake(float amplitude = 2f, float frequency = 2f, float duration = 0.25f)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine(amplitude, frequency, duration));
    }

    private IEnumerator ShakeRoutine(float amplitude, float frequency, float duration)
    {
        noise.AmplitudeGain = amplitude;
        noise.FrequencyGain = frequency;

        yield return new WaitForSeconds(duration);

        noise.AmplitudeGain = 0f;
        noise.FrequencyGain = 0f;

        shakeRoutine = null;
    }
}