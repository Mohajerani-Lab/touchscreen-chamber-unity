using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using Color = System.Drawing.Color;
using Debug = UnityEngine.Debug;
using Random = System.Random;

namespace DefaultNamespace
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public bool currentTrialEnded = false;
        public bool experimentStarted = false;
        public Camera mainCamera;

        public string RootFolder
        {
            get => rootFolder;
            private set => rootFolder = value;
        }
        // public float ObjectRotationSpeed => objectRotationSpeed;
        private struct Feedback
        {
            public int ToneFrequency { get; set; }
            public float ToneDuration { get; set; }
            public AudioSource ToneAudio { get; set; }
            public float WaitDuration { get; set; }
            public string Note { get; set; }
            public Color BackgroundColor { get; set; }
            public float BackgroundDuration { get; set; }
            public float ValveOpenDuration { get; set; }
        }

        // [SerializeField] [Range(1f, 50f)] private float objectRotationSpeed = 5f;
        [SerializeField] private string rootFolder;
        public TMP_Dropdown configDropdown;
        [SerializeField] private TextMeshProUGUI xmlContentDisplay;
        [SerializeField] public GameObject menuCanvas;
        [SerializeField] public GameObject gameCanvas;
        [SerializeField] private GameObject feedbackCanvas;
        [SerializeField] private List<GameObject> prefabs;
        [SerializeField] private int warmUpRounds = 2;
        
        private Vector3[] _spawnPoints;
        private Stack<Vector3> _uniqueSpawnPoints;
        private string _configContent;
        private Shader _shaderToReplace;
        private Random _random;
        private XElement _experimentConfig;
        private XElement _trial;
        private bool _firstTrialSucceeded;
        private Feedback _reward;
        private Feedback _punish;
        private float _orthoSize;

        private Logger _logger;
        
        

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
        }


        private void Start()
        {
            RootFolder = Application.platform == RuntimePlatform.Android ? "/storage/emulated/0/TouchScreen-Trial-Game" : Application.persistentDataPath;
            if (!Directory.Exists(RootFolder))
            {
                Debug.Log("Creating data root folder...");
                Directory.CreateDirectory(RootFolder);
            }

            Debug.Log($"Data path: {RootFolder}");

            _logger = GetComponent<Logger>();

            mainCamera = Camera.main;
            var scHeight = Screen.height;
            var scWidth = Screen.width;
            _shaderToReplace = Shader.Find("Standard");
            _random = new Random();
            var camZPos = mainCamera!.transform.position.z;
            _orthoSize = mainCamera.orthographicSize * 2;
            _spawnPoints = new[]
            {
                mainCamera!.ScreenToWorldPoint(new Vector3(scWidth/4f, scHeight / 4f, -camZPos)),
                mainCamera!.ScreenToWorldPoint(new Vector3(scWidth * 3/4f, scHeight / 4f, -camZPos)),
                mainCamera!.ScreenToWorldPoint(new Vector3(scWidth / 4f, scHeight * 3/4f, -camZPos)),
                mainCamera!.ScreenToWorldPoint(new Vector3(scWidth * 3/4f, scHeight * 3/4f, -camZPos)),
            };

            FillDropDownOpts();
        }


        private void FillDropDownOpts()
        {
            try
            {
                if (!XmlReader.ListConfigFiles().Any())
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

            foreach (var conf in XmlReader.ListConfigFiles())
            {
                configDropdown.options.Add(new TMP_Dropdown.OptionData(conf));
            }
        }

        public void UpdateXmlContent()
        {
            if (configDropdown.value == 0) return;
            var selectedFile = configDropdown.options[configDropdown.value].text;
            var filePath = $"{RootFolder}/Data/{selectedFile}";
            var xmlContentTitle = $"Contents of {selectedFile}:\n\n";
            var xmlContentBody = XmlReader.LoadXmlString(filePath);
            xmlContentDisplay.text = xmlContentTitle + xmlContentBody;
            _configContent = xmlContentBody;
            
            Debug.Log($"Loaded new configuration file: {selectedFile}");
        }

        private void ClearScene()
        {
            prefabs.Clear();
            StopAllCoroutines();
            currentTrialEnded = true;
        }

        private void ClearGameObjects()
        {
            var gameObjectsInScene = FindObjectsOfType<ObjectController>();
            foreach (var go in gameObjectsInScene)
            {
                Destroy(go.gameObject);
            }
        }

        public void SetupNewScene()
        {
            ClearGameObjects();
            ClearScene();
            menuCanvas.SetActive(false);
            gameCanvas.SetActive(true);
            StartCoroutine(ParseConfig(_configContent));
        }

        private IEnumerator InitiateExperiment()
        {
            StartCoroutine(DoWarmUp());
            yield return 0;
        }

        private void PrepareScene(XElement element, XElement cfg)
        {
            foreach (var e in element.Elements())
            {
                Func<XElement, XElement, IEnumerator> executor = e.Name.ToString() switch
                {
                    "load" => HandleLoad,
                    "call" => HandleCall,
                    "timer" => HandleTimer,
                    _ => HandleExtras
                };

                StartCoroutine(executor(e, cfg));
            }
        }

        private IEnumerator Main(XElement element, XElement cfg)
        {
            foreach (var e in element.Elements())
            {
                    Func<XElement, XElement, IEnumerator> executor = e.Name.ToString() switch
                {
                    "load" => HandleLoad,
                    "call" => HandleCall,
                    "timer" => HandleTimer,
                    "collection" => HandleCollection,
                    _ => HandleExtras
                };

                StartCoroutine(executor(e, cfg));
            }

            yield return 0;
        }

        private IEnumerator HandleLoad(XElement element, XElement cfg)
        {
            var bundleName = element.Attribute("bundle")?.Value;

            var asset = AssetBundle.LoadFromFile(Path.Combine(RootFolder, "Data", bundleName!));

            var prefab = asset.LoadAsset<GameObject>(asset.name);

            asset.Unload(false);
            
            prefab = prefab.transform.childCount > 0 ? ProcessObjectWithChildren(prefab) : ProcessSingleObject(prefab);

            prefabs.Add(prefab);
            
            yield return 0;
        }

        private GameObject ProcessSingleObject(GameObject obj)
        {
            obj.AddComponent<ObjectController>();
            obj.AddComponent<BoxCollider>();

            var rend = obj.GetComponent<MeshRenderer>();
            
            if (rend == null) return null;
            
            foreach (var material in rend.sharedMaterials)
            {
                material.shader = _shaderToReplace;
            }

            return obj;
        }

        private GameObject ProcessObjectWithChildren(GameObject obj)
        {
            obj.AddComponent<ObjectController>();

            var colliderBounds = new Bounds(Vector3.zero, Vector3.zero);

            var rendInChildren = obj.GetComponentsInChildren<MeshRenderer>();

            if (rendInChildren.Length <= 0) return null;

            foreach (var rendInChild in rendInChildren)
            {
                foreach (var material in rendInChild.sharedMaterials)
                {
                    material.shader = _shaderToReplace;
                }
            }

            var meshesInChildren = obj.GetComponentsInChildren<MeshFilter>();

            foreach (var meshFilter in meshesInChildren)
            {
                colliderBounds.Encapsulate(meshFilter.sharedMesh.bounds);
            }

            var boxCollider =  obj.AddComponent<BoxCollider>();
            boxCollider.size = colliderBounds.size;
            boxCollider.center = colliderBounds.center;
            return obj;
        }

        private IEnumerator HandleCall(XElement element, XElement cfg)
        {
            var target = element.Attribute("id")?.Value;
            var targetElement = cfg.Elements("function").ToList()
                .Find(x => x.Attribute("id")?.Value == target);
            
            yield return target switch
            {
                "issue-trial" => StartCoroutine(PerformTrial()),
                _ => StartCoroutine(HandleExtras(targetElement, _experimentConfig))
            };
        }

        private IEnumerator HandleTimer(XElement element, XElement cfg)
        {
            var duration = int.Parse(element.Attribute("duration")!.Value);
            var id = element.Attribute("id");
            yield return new WaitForSeconds(duration);
            if (id is { Value: "terminate" })
            {
                if (!experimentStarted)
                {
                    Debug.Log($"Subject did not engage in experiment after {duration} seconds, terminating.");
                    ClearScene();
                }
            }
            yield return 0;
        }

        private IEnumerator HandleSprite(XElement element, XElement cfg)
        {
            yield return 0;
        }

        public IEnumerator IssueReward()
        {
            ClearGameObjects();

            // TODO: Create sound with required frequency
            Debug.Log(_reward.Note);
            SerialComs.Instance.SendMessageToArduino($"reward{_reward.ValveOpenDuration}");
            // SerialComs.Instance.SendMessageToArduino("reward500");
            _reward.ToneAudio.Play();
            
            yield return new WaitForSeconds(_reward.ToneDuration);
            
            _reward.ToneAudio.Stop();
            
            // TODO: Connect with hardware to send drops of rewards
            
            
            yield return new WaitForSeconds(_reward.WaitDuration);
            currentTrialEnded = true;
            if (!_firstTrialSucceeded) _firstTrialSucceeded = true;
        }

        public IEnumerator IssuePunish()
        {
            ClearGameObjects();
            
            // TODO: Create sound with required frequency
            Debug.Log(_punish.Note);
            _punish.ToneAudio.Play();
            feedbackCanvas.SetActive(true);
            
            yield return new WaitForSeconds(_punish.ToneDuration);

            _punish.ToneAudio.Stop();
            feedbackCanvas.SetActive(false);
            

            yield return new WaitForSeconds(_punish.WaitDuration);
            currentTrialEnded = true;
        }

        private void SetupReward(XElement element, XElement cfg)
        {
            var position = 0;
            var samplerate = 44100;
            
            var tone = FindElementByName(element, "tone");
            var note = FindElementByName(element, "note");
            var timer = FindElementByName(element, "timer");
            var valve = FindElementByName(element, "valve");
            
            _reward.ToneFrequency = int.Parse(tone.Attribute("frequency")!.Value);
            _reward.ToneDuration = float.Parse(tone.Attribute("duration")!.Value);
            _reward.Note = note.Attribute("text")!.Value;
            _reward.WaitDuration = float.Parse(timer.Attribute("duration")!.Value);
            _reward.ValveOpenDuration = float.Parse(valve.Attribute("duration")!.Value);
            
            var clip = AudioClip.Create("RewardTone", samplerate * 20, 1, samplerate,
                true, 
                data =>
                {
                    var count = 0;
                    while (count < data.Length)
                    {
                        data[count] = Mathf.Sin(2 * Mathf.PI * _reward.ToneFrequency * position / samplerate);
                        position++;
                        count++;
                    }
                }, newPosition => position = newPosition);

            _reward.ToneAudio = gameObject.AddComponent<AudioSource>();
            _reward.ToneAudio.clip = clip;
        }

        private void SetupPunish(XElement element, XElement cfg)
        {
            var position = 0;
            var samplerate = 44100;
            
            var tone = FindElementByName(element, "tone");
            var note = FindElementByName(element, "note");
            var timer = FindElementByName(element, "timer");
            var sprite = FindElementByName(element, "sprite");
            
            _punish.ToneFrequency = int.Parse(tone.Attribute("frequency")!.Value);
            _punish.ToneDuration = float.Parse(tone.Attribute("duration")!.Value);
            _punish.Note = note.Attribute("text")!.Value;
            _punish.WaitDuration = float.Parse(timer.Attribute("duration")!.Value);
            _punish.BackgroundColor = Color.FromName(sprite.Attribute("id")!.Value);
            _punish.BackgroundDuration = float.Parse(sprite.Attribute("duration")!.Value);

            var clip = AudioClip.Create("PunishTone", samplerate * 20, 1, samplerate,
                true, 
                data =>
            {
                var count = 0;
                while (count < data.Length)
                {
                    data[count] = Mathf.Sin(2 * Mathf.PI * _punish.ToneFrequency * position / samplerate);
                    position++;
                    count++;
                }
            }, newPosition => position = newPosition);

            _punish.ToneAudio = gameObject.AddComponent<AudioSource>();
            _punish.ToneAudio.clip = clip;
        }

        private IEnumerator PerformTrial()
        {
            
            _uniqueSpawnPoints = new Stack<Vector3>(_spawnPoints.OrderBy(_ => _random.Next()));
            
            foreach (var e in _trial.Elements())
            {
                Func<XElement, XElement, IEnumerator> executor = e.Name.ToString() switch
                {
                    "collection" => HandleCollection,
                    _ => HandleExtras
                };

                yield return StartCoroutine(executor(e, _experimentConfig));
            }
        }

        private IEnumerator HandleExtras(XElement element, XElement cfg)
        {
            yield return 0;
        }

        private IEnumerator HandleObject(XElement element, XElement cfg)
        {
            var eId = element.Attribute("id")?.Value;
            var objectName = element.Attribute("bundle")?.Value.Split('/').ToList().Last();
            var spawnPoint =  _uniqueSpawnPoints.Pop();
            var obj = prefabs.Find(x => x.name == objectName);
            var go = Instantiate(obj, spawnPoint, Quaternion.Euler(0, 90, 0));
            go.transform.localScale = new Vector3(_orthoSize / 4f, _orthoSize / 4f, _orthoSize / 4f);
            
            go.GetComponent<ObjectController>().Type = eId switch
            {
                "rewarded" => ObjectController.ObjectType.Reward,
                "punished" => ObjectController.ObjectType.Punish,
                _ => ObjectController.ObjectType.Neutral
            };
            
            yield return 0;
        }

        private IEnumerator HandleGroup(XElement element, XElement cfg)
        {
            yield return 0;
        }

        private IEnumerator HandleCollection(XElement element, XElement cfg)
        {
            var elems = new List<XElement>();

            var collectionType = element.Attribute("sample")!.Value;
            
            switch (collectionType)
            {
                case "sequence":
                    elems = element.Elements().ToList();
                    break;
                case "random":
                    elems = element.Elements().OrderBy(_ => _random.Next()).Take(1).ToList();
                    break;
                case "loop":
                    var count = int.Parse(element.Attribute("count")!.Value);
                    elems = new List<XElement>();
                    for (var i = 0; i < count; i++)
                    {
                        elems.AddRange(element.Elements());
                    }
                    break;
            }


            foreach (var e in elems)
            {
                Func<XElement, XElement, IEnumerator> executor = e.Name.ToString() switch
                {
                    "call" => HandleCall,
                    "object" => HandleObject,
                    "group" => HandleGroup,
                    _ => HandleExtras
                };

                if (collectionType == "loop")
                {
                    currentTrialEnded = false;
                    StartCoroutine(executor(e, cfg));
                    yield return new WaitUntil((() => currentTrialEnded));
                }
                else
                {
                    StartCoroutine(executor(e, cfg));
                }
            }

            yield return 0;
        }

        private IEnumerator DoCorrectionLoop()
        {
            _firstTrialSucceeded = false;
            var count = 0;
            while (!_firstTrialSucceeded)
            {
                currentTrialEnded = false;
                Debug.Log($"Commencing correction loop #{++count}");
                yield return StartCoroutine(PerformTrial());
                yield return new WaitUntil(() => currentTrialEnded);
            }

            Debug.Log("Correction loop finished");
        }
        

        private IEnumerator DoWarmUp()
        {
            for (var i = 0; i < warmUpRounds; i++)
            {
                Debug.Log($"Commencing habituation reward #{i+1} from {warmUpRounds}");
                yield return StartCoroutine(IssueReward());
            }

            Debug.Log("Habituation rewards finished");

            yield return StartCoroutine(DoCorrectionLoop());
        }

        private IEnumerator ParseConfig(string xmlContent)
        {
            _experimentConfig = XElement.Parse(xmlContent);

            var rewardCfg = _experimentConfig.Elements().Where(
                e => e.Name.ToString().Equals("function") &&
                     e.Attribute("id")!.Value == "rewarded").ToArray();

            SetupReward(rewardCfg[0], _experimentConfig);
            
            var punishCfg = _experimentConfig.Elements().Where(
                e => e.Name.ToString().Equals("function") &&
                     e.Attribute("id")!.Value == "punished").ToArray();

            SetupPunish(punishCfg[0], _experimentConfig);
            
            var trialCfg = _experimentConfig.Elements().Where(
                e => e.Name.ToString().Equals("function") &&
                     e.Attribute("id")!.Value == "trial").ToArray();

            _trial = trialCfg[0];
            
            var prepFunc = _experimentConfig.Elements().Where(
                e => e.Name.ToString().Equals("function") &&
                     e.Attribute("id")!.Value == "prepare").ToArray();
            
            PrepareScene(prepFunc[0], _experimentConfig);
            
            yield return StartCoroutine(DoWarmUp());

            
            var mainFunc = _experimentConfig.Elements().Where(
                e => e.Name.ToString().Equals("function") &&
                     e.Attribute("id")!.Value == "main").ToArray();
            
            yield return StartCoroutine(Main(mainFunc[0], _experimentConfig));
        }

        public void Exit()
        {
            _logger.SaveLogsToDisk();
            Application.Quit();
        }

        private XElement FindElementByName(XElement parent, string childName)
        {
            return parent.Elements().Where(e => e.Name.ToString().Equals(childName)).ToArray()[0];
        }
    }
}