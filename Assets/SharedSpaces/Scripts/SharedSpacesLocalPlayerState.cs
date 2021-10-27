// Copyright (c) Facebook, Inc. and its affiliates.

using UnityEngine;
using Oculus.Platform;

public class SharedSpacesLocalPlayerState : MonoBehaviour
{
    public SharedSpacesCamera playerCamera;

    // these are initialized by SharedSpacesStateManager
    [HideInInspector]
    public Color color;
    [HideInInspector]
    public string username;
    [HideInInspector]
    public string applicationID;

    private void OnEnable()
    {
        DontDestroyOnLoad(this);
    }

    private void Awake()
    {
        // for the time being, force unique session ID
        applicationID = GenerateApplicationID();
    }

    public void Init(Message<Oculus.Platform.Models.User> message)
    {
        color = Random.ColorHSV();
        username = message.Data.DisplayName;
    }

    private string GenerateApplicationID()
    {
        uint id = (uint)(Random.value * uint.MaxValue);
        return id.ToString("X").ToLower();
    }
}
