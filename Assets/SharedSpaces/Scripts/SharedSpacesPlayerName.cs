// Copyright (c) Facebook, Inc. and its affiliates.

using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SharedSpacesPlayerName : MonoBehaviour
{
    public Transform usernameCanvas;
    [HideInInspector]
    public TMP_Text username;
    public Image masterIcon;

    private Transform cameraRig;

    private void OnEnable()
    {
        username = usernameCanvas.GetComponentInChildren<TMP_Text>();
        masterIcon = transform.GetComponentInChildren<Image>();
    }

    void Start()
    {
        cameraRig = Camera.main.GetComponentInParent<Transform>();
    }

    void Update()
    {
        usernameCanvas.rotation = Quaternion.LookRotation(usernameCanvas.position - cameraRig.position);
    }
}
