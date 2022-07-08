// Copyright (c) Facebook, Inc. and its affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-SharedSpaces/tree/main/Assets/SharedSpaces/LICENSE

using UnityEngine;

public class SharedSpacesPlayerColor : MonoBehaviour
{
    [SerializeField]
    private Renderer meshRenderer;

    public void UpdateColor(Color color)
    {
        foreach (Material mat in meshRenderer.materials)
        {
            mat.color = color;
        }
    }
}
