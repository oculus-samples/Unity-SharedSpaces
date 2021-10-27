// Copyright (c) Facebook, Inc. and its affiliates.

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
