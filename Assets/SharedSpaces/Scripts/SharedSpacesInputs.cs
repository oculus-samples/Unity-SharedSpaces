// Copyright (c) Facebook, Inc. and its affiliates.

using UnityEngine;
using UnityEngine.InputSystem;

public class SharedSpacesInputs : MonoBehaviour
{
    [Header("Character Input Values")]
    public Vector2 move;
    public Vector2 orbit;
    public bool jump;

    [Header("Movement Settings")]
    public bool analogMovement;

    public void OnMove(InputValue value)
    {
        move = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        jump = value.isPressed;
    }

    public void OnOrbit(InputValue value)
    {
        orbit = value.Get<Vector2>();
    }

    public void OnChangeColor(InputValue value)
    {
        GetComponent<SharedSpacesPlayerState>().SetColor();
    }
}
