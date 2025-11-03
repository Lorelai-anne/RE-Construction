using System.Collections;
using UnityEngine;
using TMPro;

// Handles the flow of turns between user and NPC participants
public class TurnManager : MonoBehaviour
{
    [System.Serializable]
    public class Participant
    {
        public GameObject character;   // The actual character GameObject in the scene
        public Light turnLight;        // A light that indicates whose turn it is
        public bool isUser;            // Whether this participant is the player or an NPC
        [TextArea] public string[] npcLines; // Dialogue lines for NPCs
        public Transform textAnchor;   // Where the text should appear for this character
    }

    public Participant[] participants;
    public int currentIndex = 0;       // Keeps track of whose turn it currently is
    public float typeSpeed = 0.05f;    // How fast text types out, letter by letter
    public TMP_Text timerText;         // Displays the countdown timer for each turn

    private Coroutine turnRoutine;
    private GameObject currentTextObj;
    private TMP_Text currentText;

    // --- Fun Fact system ---
    [TextArea]
    public string[] funFacts;
    public Transform factAnchor;
    private GameObject factObj;
    private TMP_Text factText;

    private int factIndex = 0; // Keeps track of which fact to show next

    // --- Decision system ---
    public TMP_Text decisionText;
    public int totalRoundsBeforeDecision = 4;
    private int roundsCompleted = 0;
    private bool awaitingDecision = false;

    // --- Dialogue tracking ---
    private int[] npcLineIndices; // Keeps track of each NPC’s current line

    void Start()
    {
        // Turn off all participant lights
        foreach (var p in participants)
            if (p.turnLight != null)
                p.turnLight.enabled = false;

        // Hide the decision text initially
        if (decisionText != null)
            decisionText.gameObject.SetActive(false);

        // Initialize each NPC’s dialogue index
        npcLineIndices = new int[participants.Length];

        // Start the turn loop
        StartTurns();
    }

    void Update()
    {
        // Handle user decision input (1 or 2)
        if (awaitingDecision)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                StartCoroutine(HandleDecision("Refrigerator"));
            if (Input.GetKeyDown(KeyCode.Alpha2))
                StartCoroutine(HandleDecision("Server"));
            return;
        }

        // Allow skipping the player’s turn
        if (participants[currentIndex].isUser && (Input.GetKeyDown(KeyCode.Space) || OVRInput.GetDown(OVRInput.Button.One)))
            SkipTurn();
    }

    public void StartTurns()
    {
        if (turnRoutine != null)
            StopCoroutine(turnRoutine);
        turnRoutine = StartCoroutine(TurnCycle());
    }

    private IEnumerator TurnCycle()
    {
        while (true)
        {
            Participant current = participants[currentIndex];

            // Turn off all lights
            foreach (var p in participants)
                if (p.turnLight != null)
                    p.turnLight.enabled = false;

            // Turn on current participant’s light
            if (current.turnLight != null)
                current.turnLight.enabled = true;

            float duration = 10f;

            // Show the current NPC’s next line (in order)
            if (!current.isUser && current.npcLines.Length > 0)
            {
                int lineIndex = npcLineIndices[currentIndex];
                string line = current.npcLines[lineIndex];
                ShowText(current, line);

                // Move to the next line for next time
                npcLineIndices[currentIndex] = (lineIndex + 1) % current.npcLines.Length;
            }

            // Run a countdown for the current turn
            float elapsed = 0f;
            while (elapsed < duration)
            {
                timerText.text = $"Time Left: {Mathf.Ceil(duration - elapsed)}s";
                if ((current.isUser && Input.GetKeyDown(KeyCode.Space)) || OVRInput.GetDown(OVRInput.Button.One))
                    break;
                elapsed += Time.deltaTime;
                yield return null;
            }

            // End turn cleanup
            ClearText();
            timerText.text = "";

            // Move to next participant
            currentIndex = (currentIndex + 1) % participants.Length;

            // Check if a full rotation (everyone spoke) is complete
            if (currentIndex == 0)
            {
                roundsCompleted++;

                // Show a fun fact after each rotation (sequentially)
                if (funFacts.Length > 0)
                {
                    ShowFact();
                    yield return new WaitForSeconds(4f);
                    ClearFact();
                }

                // After a certain number of rotations, enter decision phase
                if (roundsCompleted >= totalRoundsBeforeDecision)
                {
                    yield return StartCoroutine(TriggerDecisionPhase());
                    yield break;
                }
            }
        }
    }

    // Shows dialogue text above NPC
    private void ShowText(Participant npc, string line)
    {
        currentTextObj = new GameObject("NPC_Text");
        currentTextObj.transform.SetParent(npc.textAnchor, false);
        currentTextObj.transform.localPosition = Vector3.zero;

        TMP_Text text = currentTextObj.AddComponent<TextMeshPro>();
        text.fontSize = 0.3f;
        text.alignment = TextAlignmentOptions.Center;
        text.text = "";

        currentText = text;
        StartCoroutine(TypeText(line));
    }

    private IEnumerator TypeText(string line)
    {
        currentText.text = "";
        foreach (char c in line)
        {
            currentText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
    }

    private void ClearText()
    {
        if (currentTextObj != null)
        {
            Destroy(currentTextObj);
            currentTextObj = null;
            currentText = null;
        }
    }

    // Sequential fun fact system
    private void ShowFact()
    {
        if (factAnchor == null || funFacts.Length == 0)
            return;

        factObj = new GameObject("FunFact");
        factObj.transform.SetParent(factAnchor, false);

        TMP_Text t = factObj.AddComponent<TextMeshPro>();
        t.fontSize = 0.25f;
        t.color = Color.white; // white text
        t.alignment = TextAlignmentOptions.Center;

        // Show the next fact in order, looping when done
        t.text = funFacts[factIndex];
        factIndex = (factIndex + 1) % funFacts.Length;

        factText = t;
    }

    private void ClearFact()
    {
        if (factObj != null)
            Destroy(factObj);
    }

    // Decision phase logic
    private IEnumerator TriggerDecisionPhase()
    {
        awaitingDecision = true;

        if (decisionText != null)
        {
            decisionText.gameObject.SetActive(true);
            decisionText.text =
                "Power Shortage Crisis \n\n" +
                "You must choose:\n" +
                "[1] Unplug the Refrigerator\n" +
                "[2] Unplug the Server";
        }

        while (awaitingDecision)
            yield return null;
    }

    private IEnumerator HandleDecision(string choice)
    {
        awaitingDecision = false;

        if (decisionText != null)
        {
            if (choice == "Refrigerator")
                decisionText.text = "You unplug the refrigerator.\nThe AIs continue humming in the dark.";
            else
                decisionText.text = "You unplug the server.\nThe hum of data falls silent.";

            yield return new WaitForSeconds(6f);
            decisionText.text = "Simulation Complete.";
        }

        yield return new WaitForSeconds(4f);
    }

    public void SkipTurn()
    {
        StopCoroutine(turnRoutine);
        ClearText();
        timerText.text = "";
        participants[currentIndex].turnLight.enabled = false;

        currentIndex = (currentIndex + 1) % participants.Length;
        turnRoutine = StartCoroutine(TurnCycle());
    }
}
