using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Encodings.Web;

class Program
{
    static void Main(string[] args)
    {
        // Step 1: Load the JSON data
        string questionsJson = File.ReadAllText("questions.json");
        string answersJson = File.ReadAllText("answers.json");

        var questionsData = JsonNode.Parse(questionsJson);
        var answersData = JsonSerializer.Deserialize<JsonElement>(answersJson);

        // Step 2: Extract all fields with values into a dictionary
        var extractedAnswers = ExtractFields(answersData);

        // Step 3: Merge questions with answers
        MergeAnswers(questionsData, extractedAnswers);

        // Step 4: Output the result to a new JSON file
        string outputFilename = "output/file.json";
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        string outputJson = JsonSerializer.Serialize(questionsData, options);
        File.WriteAllText(outputFilename, outputJson);

        Console.WriteLine($"Processed data written to {outputFilename}");
    }

    static Dictionary<string, JsonElement> ExtractFields(JsonElement data)
    {
        var extractedFields = new Dictionary<string, JsonElement>();

        void ExtractRecursive(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Object || property.Value.ValueKind == JsonValueKind.Array)
                    {
                        ExtractRecursive(property.Value);
                    }
                    else
                    {
                        extractedFields[property.Name] = property.Value;
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    ExtractRecursive(item);
                }
            }
        }

        ExtractRecursive(data);
        return extractedFields;
    }

    static void MergeAnswers(JsonNode questions, Dictionary<string, JsonElement> answers)
    {
        ProcessNode(questions, answers);
    }

    static void ProcessNode(JsonNode node, Dictionary<string, JsonElement> answers)
    {
        if (node is JsonObject obj)
        {
            if (obj["fields"] is JsonArray fields)
            {
                ProcessFields(fields, answers);
            }

            if (obj["repeatingFields"] is JsonArray repeatingFields)
            {
                foreach (var repeatingField in repeatingFields.AsArray())
                {
                    ProcessNode(repeatingField, answers);
                }
            }

            foreach (var property in obj)
            {
                if (property.Value is JsonObject || property.Value is JsonArray)
                {
                    ProcessNode(property.Value, answers);
                }
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                ProcessNode(item, answers);
            }
        }
    }

    static void ProcessFields(JsonArray fields, Dictionary<string, JsonElement> answers)
    {
        foreach (var field in fields)
        {
            if (field is JsonObject fieldObj)
            {
                if (fieldObj["id"] is JsonValue idValue)
                {
                    string? fieldId = idValue.GetValue<string>();
                    if (fieldId != null && answers.TryGetValue(fieldId, out JsonElement answer))
                    {
                        fieldObj["answer"] = JsonValue.Create(answer.GetString());
                    }
                }

                if (fieldObj["repeatingFields"] is JsonArray repeatingFields)
                {
                    ProcessFields(repeatingFields, answers);
                }
            }
        }
    }
}