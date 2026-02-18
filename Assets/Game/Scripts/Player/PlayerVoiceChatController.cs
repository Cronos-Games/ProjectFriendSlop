using FishNet.Object;
using MetaVoiceChat.Output.AudioSource;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class PlayerVoiceChatController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] public AudioSource voiceChatOutput;
    private VcAudioSourceOutput _sourceOutput;

    public override void OnStartClient()
    {
        base.OnStartClient();

        _sourceOutput = Object.FindFirstObjectByType<VcAudioSourceOutput>(FindObjectsInactive.Include); //get voice chat output source
        _sourceOutput.gameObject.SetActive(true);
        _sourceOutput.audioSource = voiceChatOutput; //set audio source
    }
}
