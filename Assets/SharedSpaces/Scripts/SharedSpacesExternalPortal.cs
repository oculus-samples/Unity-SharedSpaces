// Copyright (c) Facebook, Inc. and its affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-SharedSpaces/tree/main/Assets/SharedSpaces/LICENSE

using System;
using Meta.XR.Samples;
using UnityEngine;
using Unity.Netcode;

[MetaCodeSample("SharedSpaces")]
public class SharedSpacesExternalPortal : MonoBehaviour
{
    private SharedSpacesApplication application;

    public UInt64 ApplicationId;
    public string DestinationAPI;
    public string DeepLinkMessage;

    public bool IsValid => ApplicationId != 0 && !string.IsNullOrWhiteSpace(DestinationAPI);

    private void Start()
    {
        application = FindObjectOfType<SharedSpacesApplication>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.GetComponent<NetworkObject>().IsOwner) return;

        if (!IsValid)
        {
            Debug.LogError($"SharedSpacesExternalPortal - {gameObject.name} doesn't have the required data.");
            return;
        }
        application.OnExternalPortalEnter(this);
    }
}
