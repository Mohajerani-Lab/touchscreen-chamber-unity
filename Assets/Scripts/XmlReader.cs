using System;
using System.IO;
using System.Xml;
using UnityEngine;
using UnityEngine.Serialization;

namespace DefaultNamespace
{
    public class XmlReader : MonoBehaviour
    {
        [SerializeField] private string rootFolder;

        private void Start()
        {
            rootFolder = Application.platform switch
            {
                RuntimePlatform.Android => "/storage/emulated/0/SmoothWalk",
                RuntimePlatform.WindowsPlayer =>
                    $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments)}/TouchScreen-Trial-Game-Data",
                RuntimePlatform.WindowsEditor =>
                    $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments)}/TouchScreen-Trial-Game-Data",
                _ => Application.persistentDataPath
            };
            
            Debug.Log($"Data path: {rootFolder}");
            if (Directory.Exists(rootFolder)) return;
            Debug.Log("Creating data root folder...");
            Directory.CreateDirectory(rootFolder);
        }

        public static void LoadXml(string path)
        {
            // var doc = new XmlDocument();
            // // doc.Load(path);
            // var elem = doc.CreateElement("bk", "genre", "urn:samples");
            // elem.InnerText = "Hi";
            // doc.DocumentElement?.AppendChild(elem);
        }

        public void ListFiles()
        {
            foreach (var file in Directory.GetFiles(rootFolder))
            {
                Debug.Log(file);
            }
        }
    }
}