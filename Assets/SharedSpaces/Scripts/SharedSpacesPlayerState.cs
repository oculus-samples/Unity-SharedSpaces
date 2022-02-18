// Copyright (c) Facebook, Inc. and its affiliates.

using UnityEngine;
using Unity.Collections;
using Unity.Netcode;

public class SharedSpacesPlayerState : NetworkBehaviour
{
    public NetworkVariable<Color> color = new NetworkVariable<Color>();
    public NetworkVariable<FixedString128Bytes> username = new NetworkVariable<FixedString128Bytes>();
    public NetworkVariable<bool> masterclient = new NetworkVariable<bool>(true);
    public SharedSpacesLocalPlayerState localPlayerState { get; private set; }

    private SharedSpacesPlayerColor playerColor;
    private SharedSpacesPlayerName playerName;
    private CharacterController characterController;

    private void OnEnable()
    {
        playerColor = GetComponent<SharedSpacesPlayerColor>();
        playerName = GetComponent<SharedSpacesPlayerName>();
        characterController = GetComponent<CharacterController>();

        color.OnValueChanged += OnColorChanged;
        username.OnValueChanged += OnUsernameChanged;
        masterclient.OnValueChanged += OnMasterclientChanged;
    }

    private void OnDisable()
    {
        color.OnValueChanged -= OnColorChanged;
        username.OnValueChanged -= OnUsernameChanged;
        masterclient.OnValueChanged -= OnMasterclientChanged;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        
        if (!localPlayerState) return;

        localPlayerState.transform.position = transform.position;
        localPlayerState.transform.rotation = transform.rotation;
    }

    private void OnColorChanged(Color oldColor, Color newColor)
    {
        playerColor.UpdateColor(newColor);
    }

    private void OnUsernameChanged(FixedString128Bytes oldName, FixedString128Bytes newName)
    {
        playerName.username.text = newName.ConvertToString();
    }

    private void OnMasterclientChanged(bool oldVal, bool newVal)
    {
        playerName.masterIcon.enabled = newVal;
    }

    private void RestoreTransform()
    {
        characterController.enabled = false;
        transform.position += localPlayerState.transform.position;
        transform.rotation  = localPlayerState.transform.rotation;
        characterController.enabled = true;
    }

    public void SetColor()
    {
        if (!localPlayerState) return;

        localPlayerState.color = Random.ColorHSV();
        SetColorServerRpc(localPlayerState.color);
    }

    private void Start()
    {
        if (!localPlayerState) return;

        localPlayerState.playerCamera.Refocus();

        SetStateServerRpc(localPlayerState.color, localPlayerState.username);
    }

    [ServerRpc]
    private void SetStateServerRpc(Color color_, string username_)
    {
        color.Value = color_;
        username.Value = username_;
    }

    [ServerRpc]
    private void SetColorServerRpc(Color color_)
    {
        color.Value = color_;
    }

    [ServerRpc]
    private void SetMasterServerRpc(bool masterclient_)
    {
        masterclient.Value = masterclient_;
    }
    /********************************************************************/

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner) return;

        if (!localPlayerState)
        {
            localPlayerState = FindObjectOfType<SharedSpacesLocalPlayerState>();
        }

        if (localPlayerState)
        {
            localPlayerState.playerCamera.Init(
                characterController,
                GetComponent<SharedSpacesInputs>()
            );

            SetStateServerRpc(localPlayerState.color, localPlayerState.username);
            SetMasterServerRpc(IsHost);
        }
    }
}
