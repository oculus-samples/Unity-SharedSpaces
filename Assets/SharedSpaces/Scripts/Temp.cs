// Copyright (c) Facebook, Inc. and its affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-SharedSpaces/tree/main/Assets/SharedSpaces/LICENSE

using Meta.XR.Samples;
using UnityEngine;

[MetaCodeSample("SharedSpaces")]
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
