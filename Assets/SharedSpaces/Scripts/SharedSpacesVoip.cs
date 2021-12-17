// Copyright (c) Facebook, Inc. and its affiliates.

using System.Collections;
using UnityEngine;
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
using UnityEngine.Android;
#endif
using Unity.Netcode;
using Photon.Voice.Unity;
using Photon.Voice.Unity.UtilityScripts;

public class SharedSpacesVoip : MonoBehaviour
{
    public GameObject sharedSpacesSpeakerPrefab;
    public GameObject sharedSpacesRecorderPrefab;

    private GameObject sharedSpacesRecorder;

    private void OnEnable()
    {
        DontDestroyOnLoad(this);
    }

    private void Start()
    {
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif
    }

    public void StartVoip(Transform parent)
    {
        sharedSpacesRecorder = Instantiate(sharedSpacesRecorderPrefab, parent);

        VoiceConnection voiceConnection = sharedSpacesRecorder.GetComponent<VoiceConnection>();
        voiceConnection.SpeakerFactory = SpeakerFactory;

        StartCoroutine(JoinPhotonVoiceRoom());
    }

    private IEnumerator JoinPhotonVoiceRoom()
    {
        yield return new WaitUntil(() => SharedSpacesSession.photonVoiceRoom != "");

#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
#endif
            ConnectAndJoin connectAndJoin = sharedSpacesRecorder.GetComponent<ConnectAndJoin>();
            connectAndJoin.RoomName = SharedSpacesSession.photonVoiceRoom;
            connectAndJoin.ConnectNow();

#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
        }
#endif
    }

    private Speaker SpeakerFactory(int playerId, byte voiceId, object userData)
    {
        SharedSpacesPlayerState player = null;
        // The mapping between Unity.Netcode client IDs and Photon client IDs is as below.
        // Check GetMlapiClientId(), line 470, of PhotonRealtimeTransport.cs
        int targetId = playerId == 1 ? 0 : playerId + 1;
        
        foreach (SharedSpacesPlayerState p in FindObjectsOfType<SharedSpacesPlayerState>())
        {
            if (p.GetComponent<NetworkObject>().OwnerClientId == (ulong)targetId)
            {
                player = p;
                break;
            }
        }
        
        Speaker speaker = Instantiate(sharedSpacesSpeakerPrefab, player.transform).GetComponent<Speaker>();
        float headHeight = speaker.GetComponentInParent<CharacterController>().height;

        speaker.transform.localPosition = new Vector3(0.0f, headHeight, 0.0f);

        return speaker;
    }
}
