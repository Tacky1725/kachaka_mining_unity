using System;
using System.Collections.Generic;
using UnityEngine;

public class JoyconDebugView : MonoBehaviour
{
    [SerializeField] private JoyconRumbleService rumbleService;
    [SerializeField] private bool showButtonDebug = true;
    [SerializeField] private bool enableDpadUpRumbleTest = true;
    [SerializeField] private float testLowFrequency = 160f;
    [SerializeField] private float testHighFrequency = 320f;
    [SerializeField, Range(0f, 1f)] private float testAmplitude = 0.6f;
    [SerializeField, Min(1)] private int testDurationMilliseconds = 200;

    private static readonly Joycon.Button[] Buttons =
        Enum.GetValues(typeof(Joycon.Button)) as Joycon.Button[];

    private List<Joycon> joycons;
    private Joycon joyconL;
    private Joycon joyconR;
    private Joycon.Button? pressedButtonL;
    private Joycon.Button? pressedButtonR;

    private void Start()
    {
        if (rumbleService == null)
        {
            rumbleService = GetComponent<JoyconRumbleService>();
        }

        RefreshJoycons();
    }

    private void Update()
    {
        pressedButtonL = null;
        pressedButtonR = null;
        RefreshJoycons();

        if (joycons == null || joycons.Count <= 0)
        {
            return;
        }

        for (int i = 0; i < Buttons.Length; i++)
        {
            Joycon.Button button = Buttons[i];
            if (joyconL != null && joyconL.GetButton(button))
            {
                pressedButtonL = button;
            }

            if (joyconR != null && joyconR.GetButton(button))
            {
                pressedButtonR = button;
            }
        }

        if (!enableDpadUpRumbleTest)
        {
            return;
        }

        bool pressedTestButton =
            (joyconL != null && joyconL.GetButtonDown(Joycon.Button.DPAD_UP)) ||
            (joyconR != null && joyconR.GetButtonDown(Joycon.Button.DPAD_UP));

        if (pressedTestButton)
        {
            if (rumbleService != null)
            {
                rumbleService.PlayRumble(testLowFrequency, testHighFrequency, testAmplitude, testDurationMilliseconds);
            }
            else
            {
                PlayDirectTestRumble();
            }
        }
    }

    private void OnGUI()
    {
        if (!showButtonDebug)
        {
            return;
        }

        GUIStyle style = GUI.skin.GetStyle("label");
        style.fontSize = 24;

        if (joycons == null || joycons.Count <= 0)
        {
            GUILayout.Label("Joy-Con is not connected.");
            return;
        }

        if (joyconL == null)
        {
            GUILayout.Label("Joy-Con (L) is not connected.");
            return;
        }

        if (joyconR == null)
        {
            GUILayout.Label("Joy-Con (R) is not connected.");
            return;
        }

        GUILayout.BeginHorizontal(GUILayout.Width(960));
        DrawJoyconStatus("Joy-Con (L)", pressedButtonL);
        DrawJoyconStatus("Joy-Con (R)", pressedButtonR);
        GUILayout.EndHorizontal();
    }

    private void RefreshJoycons()
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

    private void DrawJoyconStatus(string joyconName, Joycon.Button? pressedButton)
    {
        GUILayout.BeginVertical(GUILayout.Width(480));
        GUILayout.Label(joyconName + " pressed button: " + pressedButton);
        GUILayout.EndVertical();
    }

    private void PlayDirectTestRumble()
    {
        if (joyconL != null)
        {
            joyconL.SetRumble(testLowFrequency, testHighFrequency, testAmplitude, testDurationMilliseconds);
        }

        if (joyconR != null)
        {
            joyconR.SetRumble(testLowFrequency, testHighFrequency, testAmplitude, testDurationMilliseconds);
        }
    }
}
