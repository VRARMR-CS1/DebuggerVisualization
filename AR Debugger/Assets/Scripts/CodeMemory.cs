using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using TMPro;
using System.Text;

public class CodeMemory : MonoBehaviour
{
    public List<string> MemoryFrame = new List<string>();
    public TextMeshProUGUI MemoryFrameText;
    public GameObject Headset;
    public Dictionary<string, int> variables = new Dictionary<string, int>();
    public GameObject MovingObject;
    public GameObject movingObjectPrefab; // Assign this in the Unity inspector

    public void Start()
    {
        Debug.Log("CodeMemory Start() called");

        // Instantiate the MovingObject from the prefab
        MovingObject = Instantiate(movingObjectPrefab, Headset.transform.position + Headset.transform.forward * 2.0f - new Vector3(0, 0.5f, 0), movingObjectPrefab.transform.rotation);
        MovingObject.SetActive(true);
        Debug.Log($"Initial position of MovingObject: {MovingObject.transform.position}");

        // Create a Canvas as a child of the MovingObject
        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObject.AddComponent<CanvasRenderer>();
        canvasObject.transform.SetParent(MovingObject.transform);

        // Position the canvas in front of the higher part of the object
        canvasObject.transform.localPosition = new Vector3(1.5f, 1.5f, 2.1f); // Adjust the position as needed
        canvasObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        RectTransform rectTransform = canvasObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(13, 20);

        // Create a UI Text object and set its properties
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(canvasObject.transform);
        textObject.transform.localPosition = Vector3.zero;
        textObject.transform.localScale = Vector3.one;

        MemoryFrameText = textObject.AddComponent<TextMeshProUGUI>();
        MemoryFrameText.text = "";
        MemoryFrameText.fontSize = 3; // Adjust font size for better visibility
        MemoryFrameText.color = Color.black; // Adjust text color for better visibility
        MemoryFrameText.rectTransform.sizeDelta = new Vector2(13, 20);

        Debug.Log("MemoryFrameText initialized");

    }

    public void UpdateFrame(ASTNode node)
    {
        Debug.Log("UpdateFrame called with node: " + node);
        string varName = null;
        string value = null;

        if (node.Targets != null)
        {
            var target = node.Targets[0];
            var idProperty = target.GetType().GetProperty("Id");
            if (idProperty != null)
            {
                varName = idProperty.GetValue(target).ToString();

                if (node.Value is JObject)
                {
                    JObject jObject = node.Value as JObject;
                    Debug.Log("node.Value: " + node.Value);

                    if (jObject == null)
                    {
                        Debug.LogError("node.Value cannot be cast to JObject");
                        return;
                    }

                    JToken typeToken;
                    if (!jObject.TryGetValue("type", out typeToken))
                    {
                        Debug.LogError("JObject does not contain a 'type' property");
                        return;
                    }

                    var valueType = typeToken.ToString();
                    if (valueType == "Num")
                    {
                        value = jObject["n"].ToString();
                        variables[varName] = int.Parse(value);
                    }
                    else if (valueType == "BinOp")
                    {
                        var left = jObject["left"]["id"].ToString();
                        var op = jObject["op"]["type"].ToString();
                        int leftValue = 0;

                        if (!variables.TryGetValue(left, out leftValue))
                        {
                            Debug.LogError("Variable " + left + " not found in variables dictionary.");
                            return;
                        }

                        int rightValue = 0;

                        if (jObject["right"]["type"].ToString() == "Num")
                        {
                            rightValue = int.Parse(jObject["right"]["n"].ToString());
                        }
                        else if (jObject["right"]["type"].ToString() == "Name")
                        {
                            var right = jObject["right"]["id"].ToString();
                            if (!variables.TryGetValue(right, out rightValue))
                            {
                                Debug.LogError("Variable " + right + " not found in variables dictionary.");
                                return;
                            }
                        }
                        else
                        {
                            Debug.LogError("Unsupported right operand type in BinOp.");
                            return;
                        }

                        int result = 0;
                        if (op == "Add")
                        {
                            result = leftValue + rightValue;
                        }
                        else if (op == "Sub")
                        {
                            result = leftValue - rightValue;
                        }
                        else if (op == "Mult")
                        {
                            result = leftValue * rightValue;
                        }
                        else if (op == "Div")
                        {
                            result = leftValue / rightValue;
                        }
                        else if (op == "Mod")
                        {
                            result = leftValue % rightValue;
                        }

                        value = result.ToString();
                        variables[varName] = result;
                    }
                }
            }
        }

        if (varName != null && value != null)
        {
            for (int i = 0; i < MemoryFrame.Count; i++)
            {
                if (MemoryFrame[i].StartsWith(varName + " ="))
                {
                    MemoryFrame[i] = $"{varName} | {value}";
                    return;
                }
            }

            MemoryFrame.Add($"{varName} | {value}");
        }
    }

    public void Visualize(List<string> frame, Vector3 location)
    {
        Debug.Log("Visualize called");
        StartCoroutine(AnimateMemoryFrame(frame, location));
    }

    private IEnumerator AnimateMemoryFrame(List<string> frame, Vector3 location)
    {
        float duration = 2.0f; // Duration for each animation step
        float elapsedTime = 0f;

        Vector3 startPosition = MovingObject.transform.position;
        Vector3 endPosition = location;

        Debug.Log($"Animating from {startPosition} to {endPosition}");

        while (elapsedTime < duration)
        {
            MovingObject.transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        MovingObject.transform.position = endPosition;

        StringBuilder sb = new StringBuilder();
        foreach (string line in frame)
        {
            sb.AppendLine(line);
        }
        MemoryFrameText.text = sb.ToString();
        Debug.Log("MemoryFrameText.text: " + MemoryFrameText.text);
    }
}