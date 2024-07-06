using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public class SaveData
{
    private static string GetFilePath(string key)
    {
        return $"{Application.dataPath}/savedData/{key}.json";
    }
    public static void SaveObject<T>(string key, List<T> data)
    {
        string json = JsonConvert.SerializeObject(data, Formatting.Indented, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
        Debug.Log(data.Count);
        Debug.Log(json);
        File.WriteAllText(GetFilePath(key), json);
    }

    public static T LoadObject<T>(string key)
    {
        if (File.Exists(GetFilePath(key)))
        {
            string json = File.ReadAllText(GetFilePath(key));
            // return JsonUtility.FromJson<T>(json);
            return JsonConvert.DeserializeObject<T>(json);
        }
        return default;
    }
}
