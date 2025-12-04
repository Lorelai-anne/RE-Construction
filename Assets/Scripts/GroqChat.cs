using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

[System.Serializable]
public class GroqMessage
{
    public string role;
    public string content;
}

[System.Serializable]
public class GroqChoice
{
    public int index;
    public GroqMessage message;
}

[System.Serializable]
public class GroqResponse
{
    public List<GroqChoice> choices;
}

public class GroqChat : MonoBehaviour
{
    [Header("Personality Settings")]
    [TextArea] public string personalityPrompt = "You are a helpful AI."; 
    [TextArea] public string defaultUserPrompt = "Continue the conversation logically and keep you response to about one to two sentences and very short";

    [Header("API Settings")]
    private string apiKey = ""; 
    private string endpoint = "https://api.groq.com/openai/v1/chat/completions";
    public string model = "llama-3.1-8b-instant";

    public IEnumerator GetGroqResponse(System.Action<string> onResponse, string userPrompt = null)
    {
        string systemPrompt = personalityPrompt;
        userPrompt ??= defaultUserPrompt;

        // Manually build JSON since JsonUtility doesn't support anonymous types
        string jsonBody = 
            "{ " +
            $"\"model\": \"{model}\", " +
            "\"messages\": [" +
                $"{{ \"role\": \"system\", \"content\": \"{Escape(systemPrompt)}\" }}," +
                $"{{ \"role\": \"user\", \"content\": \"{Escape(userPrompt)}\" }}" +
            "]," +
            "\"max_tokens\": 80" + 
            "}";



        Debug.Log("Groq Request JSON:\n" + jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            Debug.LogError($"Groq API Error: {request.error}");
            Debug.LogError($"Groq Response Code: {request.responseCode}");
            Debug.LogError($"Groq Response Text: {request.downloadHandler.text}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log("Groq Raw JSON:\n" + json);

                string message = ParseGroqResponse(json);
                Debug.Log("Groq Parsed Message: " + message);
                onResponse?.Invoke(message);
            }
            else
            {
                Debug.LogError("Groq API Error: " + request.error);
                onResponse?.Invoke("(API call failed)");
            }
        }
    }

    private string ParseGroqResponse(string json)
    {
        try
        {
            var response = JsonUtility.FromJson<GroqResponse>(json);
            if (response != null && response.choices != null && response.choices.Count > 0)
            {
                return response.choices[0].message.content.Trim();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to parse response: " + e.Message);
        }
        return "(Failed to parse AI response)";
    }

    private string Escape(string text)
    {
        return text
            .Replace("\"", "\\\"")
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }
}

