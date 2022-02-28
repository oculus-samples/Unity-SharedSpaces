// Copyright (c) Facebook, Inc. and its affiliates.

using UnityEngine;
using Unity.Collections;
using Unity.Netcode;

public class SharedSpacesPlayerState : NetworkBehaviour
{
    public NetworkVariable<Color> color = new NetworkVariable<Color>();
    public NetworkVariable<FixedString128Bytes> username = new NetworkVariable<FixedString128Bytes>();
    public NetworkVariable<bool> masterclient = new NetworkVariable<bool>(true);

    private SharedSpacesPlayerColor playerColor;
    private SharedSpacesPlayerName playerName;
    private CharacterController characterController;

    private SharedSpacesLocalPlayerState LocalPlayerState => IsOwner ? SharedSpacesLocalPlayerState.Instance : null;

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
        
        if (!LocalPlayerState) return;

        LocalPlayerState.transform.position = transform.position;
        LocalPlayerState.transform.rotation = transform.rotation;

        LocalPlayerState.OnChange -= UpdateData;
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
        transform.position += LocalPlayerState.transform.position;
        transform.rotation  = LocalPlayerState.transform.rotation;
        characterController.enabled = true;
    }

    public void SetColor()
    {
        if (!LocalPlayerState) return;

        LocalPlayerState.color = Random.ColorHSV();
        SetColorServerRpc(LocalPlayerState.color);
    }

    private void Start()
    {
        OnColorChanged(color.Value, color.Value);
        OnUsernameChanged(username.Value, username.Value);
        OnMasterclientChanged(masterclient.Value, masterclient.Value);

        if (!LocalPlayerState) return;

        LocalPlayerState.playerCamera.Refocus();
        LocalPlayerState.OnChange += UpdateData;
        
        UpdateData();
    }

    private void UpdateData()
    {
        SetStateServerRpc(LocalPlayerState.color, LocalPlayerState.username);
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

        if (LocalPlayerState)
        {
            LocalPlayerState.playerCamera.Init(
                characterController,
                GetComponent<SharedSpacesInputs>()
            );

            SetStateServerRpc(LocalPlayerState.color, LocalPlayerState.username);
            SetMasterServerRpc(IsHost);
        }
    }
}
