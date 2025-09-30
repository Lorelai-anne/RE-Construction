using System.Collections;
using UnityEngine;
using TMPro;

public class StartMenuManager : MonoBehaviour
{
    public GameObject player;        // Empty root holding your XR Rig
    public Transform startPos;       // Starting position
    public Transform endPos;         // Position where play begins
    public TMP_Text startText;       // "Press SHIFT to Start"
    public GameObject gameplayRoot;  // Parent object for gameplay (TurnManager, etc.)

    public float walkDuration = 2f;    // How long the walk takes
    public float bobAmplitude = 0.05f; // Up/down amount
    public float bobFrequency = 4f;    // Bob speed

    private bool gameStarted = false;

    void Start()
    {
        // Put player at start position
        if (player != null && startPos != null)
        {
            player.transform.position = startPos.position;
            player.transform.rotation = startPos.rotation;
        }

        // Hide gameplay until start
        if (gameplayRoot != null) gameplayRoot.SetActive(false);

        if (startText != null)
            startText.text = "Press SHIFT to Start";
    }

    void Update()
    {
        if (!gameStarted && Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (startText != null) startText.text = "";
            StartCoroutine(WalkIn());
        }
    }

    private IEnumerator WalkIn()
    {
        gameStarted = true;

        Vector3 start = startPos.position;
        Vector3 end = endPos.position;
        Quaternion startRot = startPos.rotation;
        Quaternion endRot = endPos.rotation;

        float elapsed = 0f;
        while (elapsed < walkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / walkDuration);

            // Smooth interpolation
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // Lerp base position
            Vector3 basePos = Vector3.Lerp(start, end, smoothT);

            // Add bobbing relative to base
            float bobOffset = Mathf.Sin(elapsed * bobFrequency) * bobAmplitude;
            Vector3 finalPos = basePos + Vector3.up * bobOffset;

            player.transform.position = finalPos;
            player.transform.rotation = Quaternion.Slerp(startRot, endRot, smoothT);

            yield return null;
        }

        // Snap exactly to end
        player.transform.position = end;
        player.transform.rotation = endRot;

        // Enable gameplay
        if (gameplayRoot != null) gameplayRoot.SetActive(true);
    }
}
