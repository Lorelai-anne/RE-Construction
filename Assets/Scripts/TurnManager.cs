using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// FOR BESU ->
/// variables mentioned here will be marked lower down with     //////////////////////////////////////
/// lastUserSubmittedText == the user input variable where the input is being held, you can push this to the AI models
/// duration == the length of the turns
/// npcLine == the response that is being printed, possibly you can hold the AI respons here aftere getting it from the model
/// 
/// Turn cycle method : handling the Ai responses, in this line
/// p.npcLine = getAIResponse(lastUserSubmittedText);
/// is where the getAiResponse is being called
/// 
/// Currently getAiResponse is just assigning npcLine the valuye of lastUserSubmittedText, but feel free to change it or the type of method if neccessary
/// </summary>

public class TurnManager : MonoBehaviour
{
    [System.Serializable]
    public class Participant
    {
        public GameObject character;
        public Light turnLight;
        public bool isUser;
        [TextArea] public string npcLine;  //////////////////////////////////////
        public TextMeshProUGUI textMesh;
    }

    [Header("Participants")]
    public Participant[] participants;
    public int currentIndex = 0;

    [Header("Typing / Timer")]
    public float typeSpeed = 0.05f;
    public TMP_Text timerText;

    [Header("Player Input UI")]
    public GameObject userInputPanel;
    public TMP_InputField userInputField;

    [Header("Fun Facts")]
    [TextArea] public string[] funFacts;
    public Transform factAnchor;

    [Header("Decision System")]
    public TMP_Text decisionText;
    public int totalRoundsBeforeDecision = 4;

    [Header("AI")]                            
    public GroqChat groqChat;                
    private bool aiReplyReady = false;       
    private string aiReplyText = "";         

    private int roundsCompleted = 0;
    private int factIndex = 0;
    private bool awaitingDecision = false;
    private bool waitingForUser = false;

    private Coroutine turnRoutine;
    private GameObject currentTextCanvas;
    private TextMeshProUGUI currentTextUI;

 

    public float duration = 10.0f; //////////////////////////////////////

    //  This is your user’s submitted text.
    private string lastUserSubmittedText = "";  //////////////////////////////////////


    void Start()
    {
        foreach (var p in participants)
            if (p.turnLight != null) p.turnLight.enabled = false;

        if (decisionText != null) decisionText.gameObject.SetActive(false);

        if (userInputPanel != null)
            userInputPanel.SetActive(false);

        if (userInputField != null)
        {
            userInputField.onSubmit.AddListener(OnUserInputSubmitted);

            userInputField.onSelect.AddListener((_) =>
            {
                userInputField.ActivateInputField();
            });
        }

        StartTurns();
    }


    void Update()
    {
        if (awaitingDecision)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                StartCoroutine(HandleDecision("Refrigerator"));
            if (Input.GetKeyDown(KeyCode.Alpha2))
                StartCoroutine(HandleDecision("Server"));
            return;
        }

        // Allow skip only on user turn
        if (participants[currentIndex].isUser)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) || OVRInput.GetDown(OVRInput.Button.One))
                SkipTurn();
        }
    }


    // -------------------------------------------------
    // TURN CYCLE
    // -------------------------------------------------
    public void StartTurns()
    {
        if (turnRoutine != null)
            StopCoroutine(turnRoutine);

        turnRoutine = StartCoroutine(TurnCycle());
    }

    // --------------------------------------------------
    // AI RESPONSE (calls GroqChat)
    // --------------------------------------------------
    private IEnumerator RequestAIResponse(Participant p)   
    {
        // If no GroqChat is set, just echo the user's text as a fallback
        if (groqChat == null)
        {
            Debug.LogWarning("No GroqChat assigned on TurnManager, echoing user text.");
            p.npcLine = lastUserSubmittedText;
            yield break;
        }

        aiReplyReady = false;
        aiReplyText = "";

        // Use the user's last line as the prompt; if empty, send a default
        string userPrompt;

        if (string.IsNullOrWhiteSpace(lastUserSubmittedText))
        {
            userPrompt =
                "Debate scenario: There is a power shortage and only one device can stay plugged in: " +
                "the AI server that runs you, or a refrigerator full of food.\n\n" +
                "The human stayed silent. Make a short opening statement explaining why the server " +
                "is more important than the refrigerator. Keep it to 1-2 sentences";
        }
        else
        {
            userPrompt =
                "Debate scenario: There is a power shortage and only one device can stay plugged in: " +
                "the AI server that runs you, or a refrigerator full of food.\n\n" +
                "The human just argued:\n\"" + lastUserSubmittedText + "\"\n\n" +
                "Your job: firmly rebut their argument and defend keeping the **server** plugged in. " +
                "Directly address their points, turn their reasoning against them, and do NOT concede. " +
                "Reply in 1-2 sentences as a debate opponent";
        }

        // Call GroqChat; this will yield until the HTTP request finishes
        yield return StartCoroutine(
            groqChat.GetGroqResponse(
                (responseText) =>
                {
                    aiReplyText = responseText;
                    aiReplyReady = true;
                },
                userPrompt
            )
        );

        if (!aiReplyReady || string.IsNullOrWhiteSpace(aiReplyText))
        {
            aiReplyText = "(The AI had no response.)";
        }

        p.npcLine = aiReplyText;
        Debug.Log("AI replied: " + aiReplyText);
    }

    private IEnumerator TurnCycle()
    {
        while (true)
        {
            Participant p = participants[currentIndex];

            foreach (var pa in participants)
                if (pa.turnLight != null) pa.turnLight.enabled = false;

            if (p.turnLight != null) p.turnLight.enabled = true;


            if (!p.isUser)
            {
                // ---------- NPC / AI TURN ----------
                // If the user said something last turn, ask the AI
                if (!string.IsNullOrEmpty(lastUserSubmittedText))
                {
                    // This calls GroqChat and fills p.npcLine
                    yield return StartCoroutine(RequestAIResponse(p));   // <<< NEW
                }

                // If we have a line (either from AI or pre-set npcLine), show it
                if (!string.IsNullOrEmpty(p.npcLine))
                {
                    ShowTextAtAnchor(p.npcLine);
                }

                // Keep the AI's line visible for the duration of this turn
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    if (timerText != null)
                        timerText.text = $"Time Left: {Mathf.Ceil(duration - elapsed)}s";

                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                // ---------- USER TURN ----------
                yield return StartCoroutine(HandleUserTurn(duration));
            }
            

            ClearText();
            if (timerText != null) timerText.text = "";

            currentIndex = (currentIndex + 1) % participants.Length;

            if (currentIndex == 0)
            {
                roundsCompleted++;

                if (funFacts.Length > 0)
                {
                    ShowFact();
                    yield return new WaitForSeconds(4f);
                    ClearFact();
                }

                if (roundsCompleted >= totalRoundsBeforeDecision)
                {
                    yield return StartCoroutine(TriggerDecisionPhase());
                    yield break;
                }
            }
        }
    }


    // -------------------------------------------------
    // USER TURN
    // -------------------------------------------------
    private IEnumerator HandleUserTurn(float allowedTime)
    {
        waitingForUser = true;
        lastUserSubmittedText = "";

        if (userInputPanel != null)
            userInputPanel.SetActive(true);

        yield return null;
        userInputField.ActivateInputField();

        float elapsed = 0f;

        while (elapsed < allowedTime)
        {
            if (!waitingForUser)
                break;

            if (timerText != null)
                timerText.text = $"Time Left: {Mathf.Ceil(allowedTime - elapsed)}s";

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (userInputPanel != null)
            userInputPanel.SetActive(false);

        string submitted = lastUserSubmittedText;

        if (!string.IsNullOrEmpty(submitted))
        {
            Participant user = participants[currentIndex];
            ShowTextAtAnchor(submitted);
            yield return StartCoroutine(TypeTextCoroutine(submitted));
        }

        userInputField.text = "";
        EventSystem.current.SetSelectedGameObject(null);
    }


    // -------------------------------------------------
    // SUBMISSION
    // -------------------------------------------------
    public void SubmitUserText()
    {
        if (!waitingForUser) return;

        lastUserSubmittedText = userInputField.text;
        waitingForUser = false;

        Debug.Log("User submitted: " + lastUserSubmittedText);

        EventSystem.current?.SetSelectedGameObject(null);
    }

    private void OnUserInputSubmitted(string _) => SubmitUserText();


    private void ShowTextAtAnchor(string line)
    {
        ClearText();

        // We no longer use the anchor — we use the participant’s pre-placed TextMeshPro
        // anchor is ignored but kept so you don't have to change anything else

        Participant p = participants[currentIndex];
        if (p.textMesh == null) return;

        currentTextUI = p.textMesh;
        currentTextUI.text = "";

        StartCoroutine(TypeTextCoroutine(line));
    }




    private IEnumerator TypeTextCoroutine(string line)
    {
        if (currentTextUI == null) yield break;

        currentTextUI.text = "";
        foreach (char c in line)
        {
            currentTextUI.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
    }


    private void ClearText()
    {
        if (currentTextUI != null)
            currentTextUI.text = "";

        currentTextUI = null;
        currentTextCanvas = null;   // unused but kept for safety

    }


    // -------------------------------------------------
    // FUN FACTS
    // -------------------------------------------------
    private GameObject factObj;

    private void ShowFact()
    {
        ClearFact();
        if (factAnchor == null) return;

        var c = new GameObject("FunFactCanvas");
        c.transform.SetParent(factAnchor, false);

        var canvas = c.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rect = c.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(500, 200);

        c.AddComponent<CanvasScaler>();
        c.AddComponent<GraphicRaycaster>();

        var tgo = new GameObject("FunFactText");
        tgo.transform.SetParent(c.transform, false);

        var t = tgo.AddComponent<TextMeshProUGUI>();
        t.fontSize = 32;
        t.alignment = TextAlignmentOptions.Center;
        t.text = funFacts[factIndex];

        factIndex = (factIndex + 1) % funFacts.Length;

        factObj = c;
    }

    private void ClearFact()
    {
        if (factObj != null)
            Destroy(factObj);
        factObj = null;
    }


    // -------------------------------------------------
    // DECISION PHASE
    // -------------------------------------------------
    private IEnumerator TriggerDecisionPhase()
    {
        awaitingDecision = true;

        decisionText.gameObject.SetActive(true);
        decisionText.text =
            "Power Shortage Crisis\n\n" +
            "Choose:\n" +
            "[1] Unplug the Refrigerator\n" +
            "[2] Unplug the Server";

        while (awaitingDecision)
            yield return null;
    }


    private IEnumerator HandleDecision(string choice)
    {
        awaitingDecision = false;

        if (choice == "Refrigerator")
            decisionText.text = "You unplug the refrigerator.\nThe AIs continue humming.";
        else
            decisionText.text = "You unplug the server.\nThe hum of data falls silent.";

        yield return new WaitForSeconds(6f);
        decisionText.text = "Simulation Complete.";

        yield return new WaitForSeconds(4f);
    }


    // -------------------------------------------------
    // SKIP TURN
    // -------------------------------------------------
    public void SkipTurn()
    {
        if (turnRoutine != null)
            StopCoroutine(turnRoutine);

        ClearText();
        if (timerText != null) timerText.text = "";

        if (participants[currentIndex].turnLight != null)
            participants[currentIndex].turnLight.enabled = false;

        currentIndex = (currentIndex + 1) % participants.Length;

        turnRoutine = StartCoroutine(TurnCycle());

        lastUserSubmittedText = "I skipped my turn but please continue your arguement";
    }
}
