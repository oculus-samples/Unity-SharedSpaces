// Copyright (c) Facebook, Inc. and its affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-SharedSpaces/tree/main/Assets/SharedSpaces/LICENSE

using UnityEngine;
using TMPro;

public class SharedSpacesDebuggingPanel : MonoBehaviour
{
    public TMP_Text consoleLog;
    private SharedSpacesLog log;

    private void Awake()
    {
        log = FindObjectOfType<SharedSpacesLog>();
        log.SetDebuggingPanel(this);
    }
}
