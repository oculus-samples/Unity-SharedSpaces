// Copyright (c) Facebook, Inc. and its affiliates.

using UnityEngine;
using MLAPI;
using MLAPI.NetworkVariable;
using MLAPI.Messaging;

public class SharedSpacesPlayerState : NetworkBehaviour
{
    public NetworkVariableColor color = new NetworkVariableColor(new NetworkVariableSettings { WritePermission = NetworkVariablePermission.ServerOnly });
    public NetworkVariableString username = new NetworkVariableString(new NetworkVariableSettings { WritePermission = NetworkVariablePermission.ServerOnly, SendTickrate = -1.0f });
    public NetworkVariableBool masterclient = new NetworkVariableBool(new NetworkVariableSettings { WritePermission = NetworkVariablePermission.OwnerOnly });
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

    private void OnDestroy()
    {
        if (!localPlayerState) return;

        localPlayerState.transform.position = transform.position;
        localPlayerState.transform.rotation = transform.rotation;
    }

    private void OnColorChanged(Color oldColor, Color newColor)
    {
        playerColor.UpdateColor(newColor);
    }

    private void OnUsernameChanged(string oldName, string newName)
    {
        playerName.username.text = newName;
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
        // This will get set by the owner, but the OnValueChanged action only triggers if the value changes. Setting to match prefab default state (enabled).
        masterclient.Value = true;

        if (!localPlayerState) return;

        localPlayerState.playerCamera.Refocus();

        SetStateServerRpc(localPlayerState.color, localPlayerState.username);
    }

    /*****************************WORKAROUND*****************************/
    /*  These RPCs are workarounds. It seems when the write permission  */
    /*  on the network variables is set to OwnerOnly, it causes either  */
    /*  delayed synchronization or no synchronization at all. Setting   */
    /*  the write permission to ServerOnly seems to fix this issue.     */
    /*  The drawback is the need for these RPCs to ask the server to    */
    /*  update the values of the network variables.                     */
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

    public override void NetworkStart()
    {
        if (!GetComponent<NetworkObject>().IsOwner) return;

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

            color.Value = localPlayerState.color;
            username.Value = localPlayerState.username;
            SetMasterServerRpc(IsHost);
        }
    }
}
