using System.Collections;
using UnityEngine;
using TMPro;

public class TurnManager : MonoBehaviour
{
    [System.Serializable]
    public class Participant
    {
        public GameObject character;    // Capsule or user object
        public Light turnLight;         // Light above them
        public bool isUser;             // True if this is the player
        [TextArea] public string[] npcLines; // Dialogue lines for NPC
        public Transform textAnchor;    // Where to spawn text above head
    }

    public Participant[] participants;
    public int currentIndex = 0;
    public float typeSpeed = 0.05f; // seconds per character

    public TMP_Text timerText;  // Assign a TextMeshProUGUI in the Canvas

    private Coroutine turnRoutine;
    private GameObject currentTextObj;
    private TMP_Text currentText;

    void Start()
    {
        // Make sure all lights are off at the start
        foreach (var p in participants)
        {
            if (p.turnLight != null)
                p.turnLight.enabled = false;
        }

        StartTurns();
    }

    void Update()
    {
        if (participants[currentIndex].isUser && Input.GetKeyDown(KeyCode.Space))
        {
            SkipTurn();
        }
    }

    public void StartTurns()
    {
        if (turnRoutine != null) StopCoroutine(turnRoutine);
        turnRoutine = StartCoroutine(TurnCycle());
    }

    private IEnumerator TurnCycle()
    {
        while (true)
        {
            Participant current = participants[currentIndex];

            // Turn OFF all lights first
            foreach (var p in participants)
            {
                if (p.turnLight != null)
                    p.turnLight.enabled = false;
            }

            // Turn ON only current speaker's light
            if (current.turnLight != null)
                current.turnLight.enabled = true;

            float duration = current.isUser ? 10f : 10f;

            // Call ShowText at the START of an NPC's turn
            if (!current.isUser && current.npcLines.Length > 0)
            {
                string line = current.npcLines[Random.Range(0, current.npcLines.Length)];
                if (!string.IsNullOrEmpty(line))
                {
                    ShowText(current, line);
                }
            }

            // Timer loop
            float elapsed = 0f;
            int lastLoggedSecond = -1;

            while (elapsed < duration)
            {
                float remaining = Mathf.Ceil(duration - elapsed);
                timerText.text = $"Time Left: {remaining}s";

                // Debug log once per second
                int elapsedInt = Mathf.FloorToInt(elapsed);
                if (elapsedInt != lastLoggedSecond)
                {
                    Debug.Log($"[TurnManager] {current.character.name} speaking, elapsed: {elapsedInt}s");
                    lastLoggedSecond = elapsedInt;
                }

                if (current.isUser && Input.GetKeyDown(KeyCode.Space))
                    break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Cleanup
            if (current.turnLight != null)
                current.turnLight.enabled = false;
            ClearText();
            timerText.text = "";

            // Advance turn
            currentIndex = (currentIndex + 1) % participants.Length;
        }
    }


    private void ShowText(Participant npc, string line)
    {
        // Create text object if it doesn’t exist
        currentTextObj = new GameObject("NPC_Text");
        currentTextObj.transform.SetParent(npc.textAnchor, false);
        currentTextObj.transform.localPosition = Vector3.zero;

        TMP_Text text = currentTextObj.AddComponent<TextMeshPro>();
        text.fontSize = 0.3f; // Adjust size for VR
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

    public void SkipTurn()
    {
        Debug.Log("Player has skipped their turn");
        StopCoroutine(turnRoutine);
        participants[currentIndex].turnLight.enabled = false;
        ClearText();
        timerText.text = "";

        currentIndex = (currentIndex + 1) % participants.Length;
        turnRoutine = StartCoroutine(TurnCycle());
    }
}

