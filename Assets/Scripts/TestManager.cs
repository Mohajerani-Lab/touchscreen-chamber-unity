using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace DefaultNamespace
{
    public class TestManager : MonoBehaviour
    {
        public static string RootFolder;
        public Logger _logger;
        public TMP_Dropdown Dropdown;
        public TextMeshProUGUI content;
        private void Start()
        {
            RootFolder = Application.platform == RuntimePlatform.Android ? "/storage/emulated/0/TouchScreen-Trial-Game" : Application.persistentDataPath;
            _logger = GetComponent<Logger>();
            Debug.Log(Application.persistentDataPath);
            FillDropDownOptions();
        }
        
        private void FillDropDownOptions()
        {
            
            string[] files;
            try
            {
                files = XmlReader.ListConfigFiles();
                if (!files.Any())
                {
                    Debug.Log($"No config files found in data path");
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error occured in reading config files, check data path folder: {e}");
                return;
            }

            foreach (var conf in files)
            {
                Dropdown.options.Add(new TMP_Dropdown.OptionData(conf));
            }
        }
        
        public void UpdateXmlContent()
        {
            if (Dropdown.value == 0) return;
            var selectedFile = Dropdown.options[Dropdown.value].text;
            var filePath = $"{RootFolder}/Data/{selectedFile}";
            var xmlContentTitle = $"Contents of {selectedFile}:\n\n";
            var xmlContentBody = XmlReader.LoadXmlString(filePath);
            content.text = xmlContentTitle + xmlContentBody;
            
            Debug.Log($"Loaded new configuration file: {selectedFile}");
        }
    }
}