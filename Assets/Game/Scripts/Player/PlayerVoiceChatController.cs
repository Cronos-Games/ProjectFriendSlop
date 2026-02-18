using FishNet.Object;
using MetaVoiceChat.Output.AudioSource;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class PlayerVoiceChatController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] public AudioSource voiceChatOutput;
    private VcAudioSourceOutput _sourceOutput;

    private GameObject _camera;
    private AudioListener _listener;

    private void Start()
    {
        foreach (var device in Microphone.devices)
        {
            Debug.Log("Mic: " + device);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        _listener = GetComponentInChildren<AudioListener>();
        if (_listener != null)
            _listener.enabled = IsOwner;

        if (!IsOwner)
            return;

        _sourceOutput = Object.FindFirstObjectByType<VcAudioSourceOutput>(FindObjectsInactive.Include); //get voice chat output source

        if (_sourceOutput == null)
            return;

        _sourceOutput.gameObject.SetActive(true);
        _sourceOutput.audioSource = voiceChatOutput; //set audio source




    }
}
