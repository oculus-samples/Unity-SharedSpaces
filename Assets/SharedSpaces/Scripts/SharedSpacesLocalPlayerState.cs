// Copyright (c) Facebook, Inc. and its affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-SharedSpaces/tree/main/Assets/SharedSpaces/LICENSE

using UnityEngine;
using Oculus.Platform;

public class SharedSpacesLocalPlayerState : Singleton<SharedSpacesLocalPlayerState>
{
    public SharedSpacesCamera playerCamera;

    [HideInInspector]
    public Color color;
    [HideInInspector]
    public string username;
    [HideInInspector]
    public string applicationID;

    public event System.Action OnChange;

    private void OnEnable()
    {
        DontDestroyOnLoad(this);
    }

    private new void Awake()
    {
        base.Awake();
        
        // for the time being, force unique session ID
        applicationID = GenerateApplicationID();
    }

    public void Init(Message<Oculus.Platform.Models.User> message)
    {
        color = Random.ColorHSV();
        username = message.Data.DisplayName;
        OnChange?.Invoke();
    }

    private string GenerateApplicationID()
    {
        uint id = (uint)(Random.value * uint.MaxValue);
        return id.ToString("X").ToLower();
    }
}
