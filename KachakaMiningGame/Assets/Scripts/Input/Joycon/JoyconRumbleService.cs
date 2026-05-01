using System.Collections.Generic;
using UnityEngine;

public class JoyconRumbleService : MonoBehaviour
{
    [Header("Artifact Rumble")]
    [SerializeField] private bool rumbleLeft = true;
    [SerializeField] private bool rumbleRight = true;
    [SerializeField] private float lowFrequency = 160f;
    [SerializeField] private float highFrequency = 320f;
    [SerializeField, Range(0f, 1f)] private float amplitude = 0.6f;
    [SerializeField, Min(1)] private int durationMilliseconds = 300;

    private List<Joycon> joycons;
    private Joycon joyconL;
    private Joycon joyconR;

    public bool HasAnyJoycon
    {
        get
        {
            RefreshJoycons();
            return joycons != null && joycons.Count > 0;
        }
    }

    private void Start()
    {
        RefreshJoycons();
    }

    public void PlayArtifactCollectedRumble()
    {
        PlayRumble(lowFrequency, highFrequency, amplitude, durationMilliseconds);
    }

    public void PlayArtifactCollectedRumble(float artifactAmplitude, int artifactDurationMilliseconds)
    {
        float clampedAmplitude = Mathf.Clamp01(artifactAmplitude);
        int clampedDurationMilliseconds = Mathf.Max(1, artifactDurationMilliseconds);
        PlayRumble(lowFrequency, highFrequency, clampedAmplitude, clampedDurationMilliseconds);
    }

    public void PlayRumble(float lowFrequencyValue, float highFrequencyValue, float amplitudeValue, int durationMs)
    {
        RefreshJoycons();

        if (rumbleLeft && joyconL != null)
        {
            joyconL.SetRumble(lowFrequencyValue, highFrequencyValue, amplitudeValue, durationMs);
        }

        if (rumbleRight && joyconR != null)
        {
            joyconR.SetRumble(lowFrequencyValue, highFrequencyValue, amplitudeValue, durationMs);
        }
    }

    public void RefreshJoycons()
    {
        JoyconManager manager = JoyconManager.Instance;
        joycons = manager != null ? manager.j : null;
        joyconL = null;
        joyconR = null;

        if (joycons == null || joycons.Count <= 0)
        {
            return;
        }

        for (int i = 0; i < joycons.Count; i++)
        {
            Joycon joycon = joycons[i];
            if (joycon == null)
            {
                continue;
            }

            if (joycon.isLeft)
            {
                joyconL = joycon;
            }
            else
            {
                joyconR = joycon;
            }
        }
    }
}
