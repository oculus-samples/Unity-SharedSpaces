// Copyright (c) Facebook, Inc. and its affiliates.

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
