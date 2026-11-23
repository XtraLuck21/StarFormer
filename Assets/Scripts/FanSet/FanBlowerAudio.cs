using UnityEngine;

/// <summary>Starts the fan loop from the beginning on each wind activation and stops it immediately on release.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(FanBlower), typeof(AudioSource))]
public sealed class FanBlowerAudio : MonoBehaviour
{
    [SerializeField] private AudioClip windClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.35f;

    private FanBlower blower;
    private AudioSource source;

    private void Awake()
    {
        blower = GetComponent<FanBlower>();
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.volume = volume;
        source.clip = windClip;
        if (windClip != null && windClip.loadState == AudioDataLoadState.Unloaded) windClip.LoadAudioData();
    }

    private void OnEnable()
    {
        blower.BlowingChanged += SetPlayback;
        SetPlayback(blower.IsBlowing);
    }

    private void SetPlayback(bool blowing)
    {
        source.Stop();
        if (windClip == null) return;
        source.timeSamples = 0;
        if (!blowing) return;
        source.volume = volume;
        source.Play();
    }

    private void OnDisable()
    {
        blower.BlowingChanged -= SetPlayback;
        source.Stop();
        if (windClip != null) source.timeSamples = 0;
    }
}
