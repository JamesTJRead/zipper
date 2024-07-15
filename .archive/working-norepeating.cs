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
        if (questions["forms"] is JsonArray forms)
        {
            foreach (var form in forms.AsArray())
            {
                if (form["tabs"] is JsonArray tabs)
                {
                    foreach (var tab in tabs.AsArray())
                    {
                        if (tab["sections"] is JsonArray sections)
                        {
                            foreach (var section in sections.AsArray())
                            {
                                if (section["fields"] is JsonArray fields)
                                {
                                    foreach (var field in fields.AsArray())
                                    {
                                        if (field["id"] is JsonValue idValue)
                                        {
                                            string fieldId = idValue.GetValue<string>();
                                            if (answers.TryGetValue(fieldId, out JsonElement answer))
                                            {
                                                // Use the actual value instead of the raw JSON
                                                field["answer"] = JsonValue.Create(answer.GetString());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}