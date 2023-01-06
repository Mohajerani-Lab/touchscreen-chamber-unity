using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Serialization;

namespace DefaultNamespace
{
    public class XmlReader : MonoBehaviour
    {

        public static string LoadXmlString(string path)
        {
            return File.ReadAllText(path);
        }

        public static string[] ListConfigFiles()
        {
            try
            {
                return Directory.GetFiles(Path.Combine(GameManager.Instance.RootFolder, "Data"))
                    .Where(file => file.EndsWith("xml"))
                    .Select(Path.GetFileName).ToArray();
            }
            catch (UnauthorizedAccessException)
            {
                Debug.Log("Storage permission not given, change settings and run app again.");
            }

            return null;
        }
    }
}