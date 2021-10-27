// Copyright (c) Facebook, Inc. and its affiliates.

using UnityEngine;

public class Temp : MonoBehaviour
{
    private void OnEnable()
    {
        DontDestroyOnLoad(this);
    }

    private void Start()
    {
        gameObject.SetActive(false);
    }
}
