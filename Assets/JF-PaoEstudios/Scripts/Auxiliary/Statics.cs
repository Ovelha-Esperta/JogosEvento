using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class Disc
{
    public static void WriteJson<T>(T objectToWrite, string path)
    {
        string json = JsonUtility.ToJson(objectToWrite, true);
        File.WriteAllText(path, Cryptography.Encrypt(json));
    }

    public static T ReadJson<T>(string path)
    {
        string json = Cryptography.Decrypt(File.ReadAllText(path));
        return JsonUtility.FromJson<T>(json);
    }
}

public static class Cryptography
{
    public static string Encrypt(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        byte[] data = Encoding.UTF8.GetBytes(input);
        byte[] key = Encoding.UTF8.GetBytes("ILOVEBREADKEY");

        for (int i = 0; i < data.Length; i++)
            data[i] ^= key[i % key.Length];

        return Convert.ToBase64String(data);
    }

    public static string Decrypt(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        byte[] data = Convert.FromBase64String(input);
        byte[] key = Encoding.UTF8.GetBytes("ILOVEBREADKEY");

        for (int i = 0; i < data.Length; i++)
            data[i] ^= key[i % key.Length];

        return Encoding.UTF8.GetString(data);
    }
}
