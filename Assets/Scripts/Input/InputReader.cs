using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;  
using static InputController;

[CreateAssetMenu(fileName = "InputReader", menuName = "Platformer/InputReader")]
public class InputReader : ScriptableObject, IGameplayActions
{
    public event UnityAction<Vector2> Move = delegate { };
    public event UnityAction<bool> Jump = delegate { };
    public event UnityAction<bool> Dash = delegate { };
    public event UnityAction<bool> Grab = delegate { };
    

    private InputController inputActions;

    public Vector2 Direction => inputActions.Gameplay.Move.ReadValue<Vector2>();

    private void OnEnable()
    {
        if (inputActions == null)
        {
            inputActions = new InputController();
            inputActions.Gameplay.SetCallbacks((this));
        }
    }

    public void EnablePlayerActions()
    {
        inputActions.Enable();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        Move.Invoke(context.ReadValue<Vector2>());
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Started:
                Jump.Invoke(true);
                break;
            case InputActionPhase.Canceled:
                Jump.Invoke(false);
                break;
        }
    }

    public void OnGrab(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Started:
                Grab.Invoke(true);
                break;
            case InputActionPhase.Canceled:
                Grab.Invoke(false);
                break;
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Started:
                Dash.Invoke(true);
                break;
            case InputActionPhase.Canceled:
                Dash.Invoke(false);
                break;
        }
    }
}
