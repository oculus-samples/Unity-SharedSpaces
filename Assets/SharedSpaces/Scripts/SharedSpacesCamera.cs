// Copyright (c) Facebook, Inc. and its affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-SharedSpaces/tree/main/Assets/SharedSpaces/LICENSE

using UnityEngine;

public class SharedSpacesCamera : MonoBehaviour
{
    private SharedSpacesInputs inputs;
    private CharacterController character;
    private Camera cam;
    private bool justStoppedMoving;
    private float heightOffset;
    private Vector3 headPos;
    private Vector2 lastMoveDir;

    public void Init(CharacterController character_, SharedSpacesInputs inputs_)
    {
        character = character_;
        heightOffset = character.height * 1.25f;
        inputs = inputs_;

        Refocus();
    }

    public void Refocus()
    {
        headPos.x = character.transform.position.x;
        headPos.y = heightOffset;
        headPos.z = character.transform.position.z;

        transform.SetPositionAndRotation(headPos - 2.5f * transform.transform.forward, Quaternion.identity);
        transform.transform.LookAt(headPos);
    }

    void Start()
    {
        DontDestroyOnLoad(this);
        cam = Camera.main;
    }

    void Update()
    {
        if (!inputs) return;

        justStoppedMoving = lastMoveDir.magnitude > 0.0f && inputs.move.magnitude == 0.0f; 
        lastMoveDir = inputs.move;
    }

    void LateUpdate()
    {
        if (!inputs) return;

        if (justStoppedMoving)
        {
            Refocus();
        }

        transform.RotateAround(headPos, Vector3.up, -1.0f * inputs.orbit.x);
    }
}
