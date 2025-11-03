using System.Collections;
using UnityEngine;
using TMPro;

public class StartMenuManager : MonoBehaviour
{
    public GameObject player;
    public Transform startPos;
    public Transform endPos;
    public TMP_Text startText;
    public GameObject gameplayRoot;

    public float walkDuration = 2f;
    public float bobAmplitude = 0.05f;
    public float bobFrequency = 4f;

    private bool gameStarted = false;

    [Header("This is the curent scenario being used, the scenario will be put here")]
    [TextArea]
    public string[] introLines;
    public float introDelay = 2.5f; // delay between lines

    void Start()
    {
        if (player != null && startPos != null)
        {
            player.transform.position = startPos.position;
            player.transform.rotation = startPos.rotation;
        }

        if (gameplayRoot != null)
            gameplayRoot.SetActive(false);

        if (startText != null)
            startText.text = "";

        StartCoroutine(IntroSequence()); //begin intro text
    }

    private IEnumerator IntroSequence()
    {
        // Display intro lines
        foreach (string line in introLines)
        {
            startText.text = line;
            yield return new WaitForSeconds(introDelay);
        }

        // show the start prompt
        startText.text = "Press SHIFT to Start";
    }

    void Update()
    {
        if (!gameStarted && (Input.GetKeyDown(KeyCode.LeftShift) || OVRInput.GetDown(OVRInput.Button.One)))
        {
            if (startText != null)
                startText.text = "";

            StopAllCoroutines(); // stop intro text updates
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
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 basePos = Vector3.Lerp(start, end, smoothT);
            float bobOffset = Mathf.Sin(elapsed * bobFrequency) * bobAmplitude;
            Vector3 finalPos = basePos + Vector3.up * bobOffset;

            player.transform.position = finalPos;
            player.transform.rotation = Quaternion.Slerp(startRot, endRot, smoothT);

            yield return null;
        }

        player.transform.position = end;
        player.transform.rotation = endRot;

        if (gameplayRoot != null)
            gameplayRoot.SetActive(true);
    }
}
