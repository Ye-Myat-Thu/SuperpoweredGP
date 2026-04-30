using UnityEngine;

[System.Serializable]
public class VoiceSet
{
    public AudioClip[] gruntClips;
    public AudioClip[] deathClips;
    public AudioClip[] spellClips;
    public AudioClip[] moveClips;
}

public class CharacterVoice : MonoBehaviour
{
    [Header("Voice Type")]
    [SerializeField] private VoiceGender voiceGender = VoiceGender.Male;

    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource moveVoiceAudioSource;

    [Header("Voice Sets")]
    [SerializeField] private VoiceSet maleVoice;
    [SerializeField] private VoiceSet femaleVoice;

    [Header("Chances")]
    [SerializeField, Range(0f, 1f)] private float gruntChance = 0.5f;
    [SerializeField, Range(0f, 1f)] private float deathChance = 1f;
    [SerializeField, Range(0f, 1f)] private float spellChance = 0.4f;
    [SerializeField, Range(0f, 1f)] private float moveChance = 0.2f;

    [Header("Variation")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    [SerializeField] private Vector2 volumeRange = new Vector2(0.9f, 1f);

    [Header("Cooldowns")]
    [SerializeField] private float gruntCooldown = 0.35f;
    [SerializeField] private float moveVoiceCooldown = 2f;
    [SerializeField] private float spellVoiceCooldown = 0.5f;

    [Header("Move Voice")]
    [SerializeField] private float moveVoiceVolume = 1f;
    [SerializeField, Range(0f, 1.1f)] private float moveVoiceReverbMix = 0.2f;

    private float nextGruntTime;
    private float nextMoveVoiceTime;
    private float nextSpellVoiceTime;

    public VoiceGender Gender => voiceGender;

    private void Awake()
    {
        if (!audioSource)
            audioSource = GetComponent<AudioSource>();

        if (!moveVoiceAudioSource)
            moveVoiceAudioSource = audioSource;
    }

    private VoiceSet GetActiveVoiceSet()
    {
        return voiceGender == VoiceGender.Male ? maleVoice : femaleVoice;
    }

    private void TryPlayClip(AudioClip[] clips, float chance)
    {
        if (audioSource == null) return;
        if (clips == null || clips.Length == 0) return;
        if (UnityEngine.Random.value > chance) return;

        AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
        if (clip == null) return;

        audioSource.pitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip, UnityEngine.Random.Range(volumeRange.x, volumeRange.y));
    }

    public void PlayGrunt()
    {
        if (Time.time < nextGruntTime) return;
        nextGruntTime = Time.time + gruntCooldown;

        VoiceSet set = GetActiveVoiceSet();
        TryPlayClip(set.gruntClips, gruntChance);
    }

    public void PlayDeath()
    {
        VoiceSet set = GetActiveVoiceSet();
        TryPlayClip(set.deathClips, deathChance);
    }

    public void PlaySpellVoice()
    {
        if (Time.time < nextSpellVoiceTime) return;
        nextSpellVoiceTime = Time.time + spellVoiceCooldown;

        VoiceSet set = GetActiveVoiceSet();
        TryPlayClip(set.spellClips, spellChance);
    }

    public void PlayMoveVoice()
    {
        if (Time.time < nextMoveVoiceTime) return;

        VoiceSet set = GetActiveVoiceSet();
        if (set == null || set.moveClips == null || set.moveClips.Length == 0) return;
        if (UnityEngine.Random.value > moveChance) return;
        if (moveVoiceAudioSource == null) return;

        nextMoveVoiceTime = Time.time + moveVoiceCooldown;

        AudioClip clip = set.moveClips[UnityEngine.Random.Range(0, set.moveClips.Length)];
        if (clip == null) return;

        moveVoiceAudioSource.pitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
        moveVoiceAudioSource.reverbZoneMix = moveVoiceReverbMix;
        moveVoiceAudioSource.PlayOneShot(clip, moveVoiceVolume);
    }

    public void StopMoveVoice()
    {
        nextMoveVoiceTime = Time.time + moveVoiceCooldown;

        if (moveVoiceAudioSource != null && moveVoiceAudioSource.isPlaying)
            moveVoiceAudioSource.Stop();
    }
}
