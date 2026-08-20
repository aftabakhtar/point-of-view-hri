using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

public class JsonManager<T>
{
    /// <summary>
    /// Reads a JSON file from the specified path and deserializes it into an object of type T.
    /// </summary>
    public static T ReadJson(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"File not found: {filePath}");
            return default;
        }

        try
        {
            string jsonData = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<T>(jsonData);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error reading JSON file: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// Serializes an object of type T and writes it to a JSON file at the specified path.
    /// </summary>
    public static void WriteJson(string filePath, T data)
    {
        try
        {
            string jsonData = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(filePath, jsonData);
            Debug.Log($"Data successfully written to: {filePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error writing JSON file: {ex.Message}");
        }
    }
}