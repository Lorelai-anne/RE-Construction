using UnityEngine;
using Meta.WitAi.Dictation;
using Meta.WitAi.Dictation.Events;

public class VoiceToTextManager : MonoBehaviour
{
    public static VoiceToTextManager Instance;

    public DictationService dictation;
    private TurnManager turnManager;

    private bool isDictating = false;

    void Awake()
    {
        Instance = this;
    }

    public void Initialize(TurnManager tm)
    {
        turnManager = tm;

#if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("VoiceToText: Running on Quest (Android) = Voice mode enabled.");
#else
        Debug.Log("VoiceToText: Running on Desktop = Textbox mode only.");
#endif
    }

    void OnEnable()
    {
        if (dictation != null)
        {
            dictation.DictationEvents.OnFullTranscription.AddListener(OnFullText);
        }
    }

    void OnDisable()
    {
        if (dictation != null)
        {
            dictation.DictationEvents.OnFullTranscription.RemoveListener(OnFullText);
        }
    }

    // Called when dictation finishes a full sentence
    private void OnFullText(string text)
    {
        if (!isDictating) return;

        Debug.Log("Dictated Text: " + text);

        isDictating = false;
        dictation.Deactivate();

        turnManager.SubmitDictatedText(text);
    }

    // Called every frame by TurnManager while waiting for user input
    public void UpdateDictation()
    {
#if UNITY_ANDROID && !UNITY_EDITOR

        // Hold INDEX trigger to talk
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
        {
            Debug.Log("Dictation Started...");
            isDictating = true;
            dictation.Activate();
        }

        // Release trigger to stop dictation
        if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger))
        {
            Debug.Log("Dictation Ended...");
            isDictating = false;
            dictation.Deactivate();
        }

#endif
    }
}
