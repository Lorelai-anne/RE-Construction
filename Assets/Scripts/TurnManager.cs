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
        public Transform textAnchor;
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
            if (Input.GetKeyDown(KeyCode.Space))
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
    // AI RESPONSE
    // --------------------------------------------------
    private string getAIResponse(string userInput) //////////////////////////////////////
    {
        string response = userInput;

        return response;
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
                if (!string.IsNullOrEmpty(lastUserSubmittedText))
                    p.npcLine = getAIResponse(lastUserSubmittedText); //////////////////////////////////////
                Debug.Log("AI said " + lastUserSubmittedText);

                if (!string.IsNullOrEmpty(p.npcLine))
                    ShowTextAtAnchor(p.textAnchor, p.npcLine);
                Debug.Log("AI said " + p.npcLine);

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
            ShowTextAtAnchor(user.textAnchor, submitted);
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


    private void ShowTextAtAnchor(Transform anchor, string line)
    {
        ClearText();
        if (anchor == null) return;

        // Create Canvas
        var canvasGO = new GameObject("DialogueCanvas");
        canvasGO.transform.SetParent(anchor, false);

        // Match rotation and scale of anchor
        canvasGO.transform.localRotation = Quaternion.identity; // aligns with anchor rotation
        canvasGO.transform.localScale = Vector3.one * 0.01f;     // scale down so text isn't huge

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var cRect = canvasGO.GetComponent<RectTransform>();
        cRect.sizeDelta = new Vector2(600, 300);

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Create Text
        var textGO = new GameObject("DialogueText");
        textGO.transform.SetParent(canvasGO.transform, false);
        textGO.transform.localRotation = Quaternion.identity; // align text with canvas
        textGO.transform.localScale = Vector3.one;

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;

        // Stretch text to fit canvas
        var tRect = textGO.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;

        currentTextCanvas = canvasGO;
        currentTextUI = tmp;

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
        if (currentTextCanvas != null)
            Destroy(currentTextCanvas);
        currentTextCanvas = null;
        currentTextUI = null;
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
    }
}
