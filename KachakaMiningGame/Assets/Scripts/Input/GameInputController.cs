using System;
using System.Collections.Generic;
using UnityEngine;

public enum JoyconSide
{
    Any,
    Left,
    Right,
}

[Serializable]
public class GameInputBinding
{
    [SerializeField]
    private GameInputAction action = GameInputAction.Confirm;

    [SerializeField]
    private KeyCode key = KeyCode.None;

    [SerializeField]
    private bool useJoyconButton = true;

    [SerializeField]
    private JoyconSide joyconSide = JoyconSide.Any;

    [SerializeField]
    private Joycon.Button joyconButton = Joycon.Button.PLUS;

    public GameInputAction Action => action;

    public GameInputBinding() { }

    public GameInputBinding(
        GameInputAction action,
        KeyCode key,
        bool useJoyconButton,
        JoyconSide joyconSide,
        Joycon.Button joyconButton
    )
    {
        this.action = action;
        this.key = key;
        this.useJoyconButton = useJoyconButton;
        this.joyconSide = joyconSide;
        this.joyconButton = joyconButton;
    }

    public bool GetButtonDown(Joycon joyconL, Joycon joyconR)
    {
        if (key != KeyCode.None && Input.GetKeyDown(key))
        {
            return true;
        }

        if (!useJoyconButton)
        {
            return false;
        }

        switch (joyconSide)
        {
            case JoyconSide.Left:
                return joyconL != null && joyconL.GetButtonDown(joyconButton);
            case JoyconSide.Right:
                return joyconR != null && joyconR.GetButtonDown(joyconButton);
            case JoyconSide.Any:
            default:
                return (joyconL != null && joyconL.GetButtonDown(joyconButton))
                    || (joyconR != null && joyconR.GetButtonDown(joyconButton));
        }
    }

    public bool GetButton(Joycon joyconL, Joycon joyconR)
    {
        if (key != KeyCode.None && Input.GetKey(key))
        {
            return true;
        }

        if (!useJoyconButton)
        {
            return false;
        }

        switch (joyconSide)
        {
            case JoyconSide.Left:
                return joyconL != null && joyconL.GetButton(joyconButton);
            case JoyconSide.Right:
                return joyconR != null && joyconR.GetButton(joyconButton);
            case JoyconSide.Any:
            default:
                return (joyconL != null && joyconL.GetButton(joyconButton))
                    || (joyconR != null && joyconR.GetButton(joyconButton));
        }
    }
}

[DisallowMultipleComponent]
public class GameInputController : MonoBehaviour
{
    [SerializeField]
    private List<GameInputBinding> bindings = CreateDefaultBindings();

    private List<Joycon> joycons;
    private Joycon joyconL;
    private Joycon joyconR;
    private readonly Dictionary<GameInputAction, bool> consumedActions =
        new Dictionary<GameInputAction, bool>();

    private void Awake()
    {
        RefreshJoycons();
    }

    private void OnValidate()
    {
        if (bindings == null || bindings.Count == 0)
        {
            bindings = CreateDefaultBindings();
        }
    }

    public bool GetButtonDown(GameInputAction action)
    {
        RefreshJoycons();

        if (bindings == null)
        {
            return false;
        }

        bool isPressed = IsActionPressed(action);
        if (!isPressed)
        {
            consumedActions[action] = false;
            return false;
        }

        bool wasConsumed;
        if (consumedActions.TryGetValue(action, out wasConsumed) && wasConsumed)
        {
            return false;
        }

        for (int i = 0; i < bindings.Count; i++)
        {
            GameInputBinding binding = bindings[i];
            if (binding != null && binding.Action == action && binding.GetButtonDown(joyconL, joyconR))
            {
                consumedActions[action] = true;
                return true;
            }
        }

        return false;
    }

    private bool IsActionPressed(GameInputAction action)
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            GameInputBinding binding = bindings[i];
            if (binding != null && binding.Action == action && binding.GetButton(joyconL, joyconR))
            {
                return true;
            }
        }

        return false;
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

    private static List<GameInputBinding> CreateDefaultBindings()
    {
        return new List<GameInputBinding>
        {
            new GameInputBinding(
                GameInputAction.Confirm,
                KeyCode.A,
                true,
                JoyconSide.Any,
                Joycon.Button.PLUS
            ),
            new GameInputBinding(
                GameInputAction.Quit,
                KeyCode.Q,
                true,
                JoyconSide.Any,
                Joycon.Button.MINUS
            ),
        };
    }
}
