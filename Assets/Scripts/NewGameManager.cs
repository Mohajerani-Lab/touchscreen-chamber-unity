using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

namespace DefaultNamespace
{
    public class NewGameManager : MonoBehaviour
    {
        
        public bool CurrentTrialEnded { get; set; } = false;
        public bool experimentStarted = false;

        public bool PunishOnEmpty { get; private set; } = true;
        public bool InputReceived { get; set; } = false;
        
        private bool _correctionLoopActive;
        public bool InitialRewardsActive { get; private set; }
        public int InitialRewardsCount { get; private set; } = 0;

        public bool CueActive { get; private set; }
        private bool _cueGiven = false;
        public bool NoInputRequired { get; private set; } = false;
        public string NoInputAction { get; private set; }
        public float NoInputWait { get; private set; }
        
        public int SectionCount { get; private set; }
        public Camera MainCamera { get; private set; }
        public TMP_Dropdown configDropdown;


        public string RootFolder
        {
            get => rootFolder;
            private set => rootFolder = value;
        }
        public class SpawnPoint
        {
            public SpawnPoint(Vector3 pos, WindowController window)
            {
                Pos = pos;
                Window = window;
                Window.Type = ObjectType.Neutral;
            }

            public Vector3 Pos { get; set; }
            public WindowController Window { get; set; }
        }

        [SerializeField] private string rootFolder;
        [SerializeField] private TextMeshProUGUI xmlContentDisplay;
        [SerializeField] public GameObject menuCanvas;
        [SerializeField] private GameObject dualGameCanvas;
        [SerializeField] private GameObject quadGameCanvas;
        [SerializeField] public GameObject feedbackCanvas;
        [SerializeField] private Shader bundleShader;
        [SerializeField] public List<GameObject> prefabs;
        [SerializeField] private WindowController[] quadWindows;
        [SerializeField] private WindowController[] dualWindows;
        
        public SpawnPoint[] SpawnPoints { get; private set; }
        // private Stack<SpawnPoint> _uniqueSpawnPoints;
        public Random Rand;
        private XElement _experimentConfig;
        public XElement TrialData { get; private set; }
        public FeedbackObject Cue { get; private set; }
        public AudioSource AudioSource { get; private set; }
        private string _configContent;
        
        public bool FirstTrialSucceeded { get; set; }
        public bool TrialSucceeded { get; set; }
        public bool RepeatTrial { get; set; } = false;
        
        private GameObject _gameCanvas;


        
        public bool DivsActive { get; private set; }
        
        public Color DivColor { get; private set; }

        private float _orthoSize;
        private float _orthoSizeWidth;

        public float OrthoSize
        {
            get => _orthoSize;
            set => _orthoSize = value;
        }


        private Logger _logger;

        private long start;
        
        
        // New vars
        public static NewGameManager Instance { get; private set; }
        private TrialManager T;
        private FeedbackManager F;
        public XElement ExperimentData { get; private set; }
        public int ExperimentTrialCount { get; private set; }
        private Timer DestroyTimer;
        public Timer Timer { get; private set; }
        public Timer ExperimentTimer { get; private set; }
        public ExperimentPhase ExperimentPhase;
        public FeedbackObject Reward { get; private set; }
        public FeedbackObject Punish { get; private set; }
        
        public List<XElement> TrialEvents { get; private set; }

        private Stack<int> _uniqueSpawnPositions;
        private int[] _lastUniqueSpawnPositions;
        // private int[] _tempSpawnPositions;
        private bool _isSimilarToPrevious;
        private int _similarToPreviousCnt;


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
            T = TrialManager.Instance;
            F = FeedbackManager.Instance;
            
            ExperimentPhase = ExperimentPhase.Preprocess;
            
            RootFolder = Application.platform == RuntimePlatform.Android ? "/storage/emulated/0/TouchScreen-Trial-Game" : Application.persistentDataPath;
            if (!Directory.Exists(RootFolder))
            {
                Debug.Log("Creating data root folder...");
                Directory.CreateDirectory(RootFolder);
            }

            Debug.Log($"Data path: {RootFolder}");

            _logger = GetComponent<Logger>();

            Rand = new Random();

            MainCamera = Camera.main;
            _orthoSize = MainCamera!.orthographicSize * 2;

            AudioSource = gameObject.AddComponent<AudioSource>();

            FillDropDownOptions();
            Timer = new Timer();
            DestroyTimer = new Timer();
            ExperimentTimer = new Timer();
            _uniqueSpawnPositions = new Stack<int>();
            _lastUniqueSpawnPositions = new[] { -1, -1, -1, -1 };
        }

        private void Update()
        {
            switch (ExperimentPhase)
            {
                case ExperimentPhase.Preprocess:
                    break;
                case ExperimentPhase.Setup:
                    ParseConfig(_configContent);
                    break;
                default:
                    CheckDestroyTimer();
                    break;
            }
            
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
        
        public void ResetSpawnWindows()
        {
            foreach (var spawnPoint in SpawnPoints)
            {
                spawnPoint.Window.Type = ObjectType.Neutral;
            }
        }

        private void ClearScene()
        {
            prefabs.Clear();
                        
            dualGameCanvas.SetActive(false);
            quadGameCanvas.SetActive(false);

            T.InitialSetup();
            F.InitialSetup();
            
            DestroyTimer.Clear();
            Timer.Clear();

            PunishOnEmpty = true;
            InputReceived = false;
            CueActive = false;
            NoInputRequired = false;
            RepeatTrial = false;
            InitialRewardsCount = 0;
        }

        public void ClearGameObjects()
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
            ExperimentPhase = ExperimentPhase.Setup;
        }

        private void SetupCanvas()
        {
            menuCanvas.SetActive(false);
            _gameCanvas.SetActive(true);
            
            foreach (var divider in
                     _gameCanvas.GetComponentsInChildren<Image>().ToList().FindAll
                         (x => x.GetComponent<WindowController>() is null))
            {
                divider.color = DivColor;
            }

        }
        
        private void ParseConfig(string xmlContent)
        {
            _experimentConfig = XElement.Parse(xmlContent);

            var rewardCfg = _experimentConfig.Elements().Where(
                e => e.Name.ToString().Equals("function") &&
                     e.Attribute("id")!.Value == "rewarded").ToArray();
            
            if (rewardCfg.Length > 0)
            {
                SetupReward(rewardCfg[0]);
            }
            
            var punishCfg = _experimentConfig.Elements().Where(
                e => e.Name.ToString().Equals("function") &&
                     e.Attribute("id")!.Value == "punished").ToArray();

            if (punishCfg.Length > 0)
            {
                SetupPunish(punishCfg[0], _experimentConfig);
            }
            
            var cueCfg = _experimentConfig.Elements().Where(
                e => e.Name.ToString().Equals("function") &&
                     e.Attribute("id")!.Value == "cued").ToArray();
            
            if (cueCfg.Length > 0)
            {
                CueActive = true;
                SetupCue(cueCfg[0]);
            }

            var trialCfg = _experimentConfig.Elements().Where(
                e => e.Name.ToString().Equals("function") &&
                     e.Attribute("id")!.Value == "trial").ToArray();

            TrialData = trialCfg[0];
            
            var prepFunc = _experimentConfig.Elements().Where(
                e => e.Name.ToString().Equals("function") &&
                     e.Attribute("id")!.Value == "prepare").ToArray();
            
            PrepareScene(prepFunc[0]);
            
            SetupCanvas();
            
            var mainFunc = _experimentConfig.Elements().Where(
                e => e.Name.ToString().Equals("function") &&
                     e.Attribute("id")!.Value == "main").ToArray();

            // ExperimentData = mainFunc[0];
            SetupExperiment(mainFunc[0]);
            ExperimentPhase = ExperimentPhase.Cue;
        }

        private void SetupExperiment(XElement element)
        {
            var collection = Utils.FindElementByName(element, "collection");
            TrialEvents = HandleCollection(collection);
        }
        
        public List<XElement> HandleCollection(XElement element)
        {

            var collectionElements = new List<XElement>();

            var innerElements = element.Elements().ToArray();
            
            var collectionType = element.Attribute("sample")!.Value;
            
            switch (collectionType)
            {
                case "sequence":
                    collectionElements.AddRange(innerElements);
                    break;
                case "random":
                    var rndIdx = Rand.Next(innerElements.Count());
                    collectionElements.Add(innerElements[rndIdx]);
                    break;
                case "loop":
                    var count = int.Parse(element.Attribute("count")!.Value);
                    for (var i = 0; i < count; i++)
                    {
                        collectionElements.AddRange(innerElements);
                    }
                    break;
            }

            return collectionElements;

        }
        
        private void SetupReward(XElement element)
        {
            var tone = Utils.FindElementByName(element, "tone");
            var note = Utils.FindElementByName(element, "note");
            var timer = Utils.FindElementByName(element, "timer");
            var valve = Utils.FindElementByName(element, "valve");
            
            
            var toneFrequency = int.Parse(tone.Attribute("frequency")!.Value);
            var toneDuration = float.Parse(tone.Attribute("duration")!.Value);
            var noteString = note.Attribute("text")!.Value;
            var waitDuration = float.Parse(timer.Attribute("duration")!.Value);
            var valveOpenDuration = float.Parse(valve.Attribute("duration")!.Value);
            
            var position = 0;
            var freq  = 44100;
            
            var clipLen = freq * toneDuration;
            
            var clip = AudioClip.Create("RewardTone", (int) clipLen, 1, freq ,
                true, 
                data =>
                {
                    var count = 0;
                    while (count < data.Length)
                    {
                        data[count] = Mathf.Sin(2 * Mathf.PI * toneFrequency * position / freq );
                        position++;
                        count++;
                    }
                }, newPosition => position = newPosition);

            Reward = new FeedbackObject(
                noteString, toneFrequency, toneDuration, waitDuration, clip, valveOpenDuration
                );
        }
        
        private void SetupCue(XElement element)
        {
            CueActive = true;
            
            var tone = Utils.FindElementByName(element, "tone");
            var note = Utils.FindElementByName(element, "note");
            var timer = Utils.FindElementByName(element, "timer");
            var valve = Utils.FindElementByName(element, "valve");
            
            
            var toneFrequency = int.Parse(tone.Attribute("frequency")!.Value);
            var toneDuration = float.Parse(tone.Attribute("duration")!.Value);
            var noteString = note.Attribute("text")!.Value;
            var waitDuration = float.Parse(timer.Attribute("duration")!.Value);
            var valveOpenDuration = float.Parse(valve.Attribute("duration")!.Value);
            
            var position = 0;
            var freq  = 44100;
            
            var clipLen = freq * toneDuration;
            
            var clip = AudioClip.Create("RewardTone", (int) clipLen, 1, freq ,
                true, 
                data =>
                {
                    var count = 0;
                    while (count < data.Length)
                    {
                        data[count] = Mathf.Sin(2 * Mathf.PI * toneFrequency * position / freq );
                        position++;
                        count++;
                    }
                }, newPosition => position = newPosition);

            Cue = new FeedbackObject(
                noteString, toneFrequency, toneDuration, waitDuration, clip, valveOpenDuration
            );
        }

        private void SetupPunish(XElement element, XElement cfg)
        {
            var tone = Utils.FindElementByName(element, "tone");
            var note = Utils.FindElementByName(element, "note");
            var timer = Utils.FindElementByName(element, "timer");
            var sprite = Utils.FindElementByName(element, "sprite");
            
            var toneFrequency = int.Parse(tone.Attribute("frequency")!.Value);
            var toneDuration = float.Parse(tone.Attribute("duration")!.Value);
            var noteString = note.Attribute("text")!.Value;
            var waitDuration = float.Parse(timer.Attribute("duration")!.Value);
            var spriteColor = sprite.Attribute("color")!.Value.Split(',')
                .Select(x => float.Parse(x) / 255).ToArray();
            var backgroundColor = new Color(spriteColor[0], spriteColor[1], spriteColor[2]);
            var backgroundDuration = float.Parse(sprite.Attribute("duration")!.Value);
            
            var position = 0;
            var freq  = 44100;
            
            var clipLen = freq * toneDuration;

            var clip = AudioClip.Create("PunishTone", (int) clipLen, 1, freq,
                true, 
                data =>
            {
                var count = 0;
                while (count < data.Length)
                {
                    data[count] = Mathf.Sin(2 * Mathf.PI * toneFrequency * position / freq);
                    position++;
                    count++;
                }
            }, newPosition => position = newPosition);

            Punish = new FeedbackObject(
                noteString, toneFrequency, toneDuration, waitDuration, clip, backgroundColor, backgroundDuration
                );
            
            feedbackCanvas.GetComponentInChildren<Image>().color = Punish.BackgroundColor;
        }

        private void PrepareScene(XElement element)
        {
            foreach (var e in element.Elements())
            {
                switch (e.Name.ToString())
                {
                    case "initial-rewards":
                        InitialRewardsActive = bool.Parse(e.Attribute("active")!.Value);
                        if (!InitialRewardsActive) break;
                        InitialRewardsCount = int.Parse(e.Attribute("count")!.Value);
                        break;
                    case "correction-loop":
                        _correctionLoopActive = bool.Parse(e.Attribute("active")!.Value);
                        break;
                    case "punish-on-empty":
                        PunishOnEmpty = bool.Parse(e.Attribute("active")!.Value);
                        break;
                    case "no-input":
                        NoInputRequired = bool.Parse(e.Attribute("active")!.Value);
                        if (!NoInputRequired) break;
                        NoInputAction = e.Attribute("action")!.Value;
                        NoInputWait = float.Parse(e.Attribute("wait-between")!.Value);
                        break;
                    case "sections":
                        SectionCount = int.Parse(e.Attribute("count")!.Value);
                        _gameCanvas = SectionCount == 2 ? dualGameCanvas : quadGameCanvas;
                        
                        var camZPos = MainCamera!.transform.position.z;
                        var scHeight = Screen.height;
                        var scWidth = Screen.width;
                        
                        SpawnPoints = SectionCount == 4 ? new []
                            {
                                new SpawnPoint(MainCamera!.ScreenToWorldPoint
                                    (new Vector3(scWidth / 4f, scHeight * 3/4f, -camZPos)), quadWindows[0]),
                                new SpawnPoint(MainCamera!.ScreenToWorldPoint
                                    (new Vector3(scWidth * 3/4f, scHeight * 3/4f, -camZPos)), quadWindows[1]),
                                new SpawnPoint(MainCamera!.ScreenToWorldPoint
                                    (new Vector3(scWidth / 4f, scHeight / 4f, -camZPos)), quadWindows[2]),
                                new SpawnPoint(MainCamera!.ScreenToWorldPoint
                                    (new Vector3(scWidth * 3/4f, scHeight / 4f, -camZPos)), quadWindows[3]),
                            } :
                            new []
                            {
                                new SpawnPoint(MainCamera!.ScreenToWorldPoint
                                    (new Vector3(scWidth / 4f, scHeight / 2f, -camZPos)), dualWindows[0]),
                                new SpawnPoint(MainCamera!.ScreenToWorldPoint
                                    (new Vector3(scWidth * 3/4f, scHeight / 2f, -camZPos)), dualWindows[1]) 
                            };
                        
                        break;
                    case "dividers":
                        DivsActive = bool.Parse(e.Attribute("active")!.Value);
                        if (!DivsActive)
                        {
                            DivColor = MainCamera.backgroundColor;
                            break;
                        }
                        var divColor = e.Attribute("color")!.Value.Split(',').
                            Select(x => float.Parse(x) / 255).ToArray();
                        DivColor = new Color(divColor[0], divColor[1], divColor[2]);
                        break;
                    case "sprite":
                        var spriteColor = e.Attribute("color")!.Value.Split(',')
                            .Select(x => float.Parse(x) / 255).ToArray();
                        MainCamera.backgroundColor = new Color(spriteColor[0], spriteColor[1], spriteColor[2]);
                        break;
                    case "load":
                        HandleLoad(e);
                        break;
                    case "timer":
                        HandleDestroyTimer(e);
                        break;
                }
            }
        }

        private void HandleDestroyTimer(XElement element)
        {
            if (DestroyTimer._started) return;
            
            var duration = int.Parse(element.Attribute("duration")!.Value);
            var id = element.Attribute("id");
            if (id is { Value: "terminate" })
                DestroyTimer.Begin(duration);

        }

        private void CheckDestroyTimer()
        {
            if (!DestroyTimer.IsFinished()) return;

            Debug.Log("Termination time passed, ending experiment");
            SaveAndExit();
        }

        private void HandleLoad(XElement element)
        {
            var bundleName = element.Attribute("bundle")?.Value;

            var asset = AssetBundle.LoadFromFile(Path.Combine(RootFolder, "Data", bundleName!));

            var prefab = asset.LoadAsset<GameObject>(asset.name);

            asset.Unload(false);

            prefab = ProcessObject(prefab);
            
            prefabs.Add(prefab); 
        }
        
        private GameObject ProcessObject(GameObject obj)
        {
            obj.AddComponent<ObjectController>();
            
            if (obj.transform.childCount == 0)
            {

                var rend = obj.GetComponent<MeshRenderer>();
            
                if (rend == null) return null;
            
                foreach (var material in rend.sharedMaterials)
                {
                    material.shader = bundleShader;
                }
                
                if (SectionCount != 2) return obj;
                
                obj.AddComponent<BoxCollider>();
                
                return obj;
            }
            
            var rendInChildren = obj.GetComponentsInChildren<MeshRenderer>();
            
            if (rendInChildren.Length <= 0) return null;

            foreach (var rendInChild in rendInChildren)
            {
                foreach (var material in rendInChild.sharedMaterials)
                {
                    material.shader = bundleShader;
                }

                if (SectionCount == 2)
                {
                    rendInChild.gameObject.AddComponent<BoxCollider>();
                }
            }

            return obj;
        }
        
        public void GenerateNewPositions()
        {
            if (RepeatTrial)
            {
                _uniqueSpawnPositions.Clear();
                for (var i = 0; i < SectionCount; i++)
                {
                    _uniqueSpawnPositions.Push(_lastUniqueSpawnPositions[i]);
                }
                return;
            };
            
            ResetSpawnWindows();
            
            while (true)
            {
                _uniqueSpawnPositions.Clear();
                
                var tempArray = Enumerable.Range(0, SectionCount).OrderBy(_ => Rand.Next()).ToArray();
                
                for (var i = 0; i < SectionCount; i++)
                {
                    if (!tempArray[i].Equals(_lastUniqueSpawnPositions[i])) continue;
                    _isSimilarToPrevious = true;
                    break;
                }
                
                if (!_isSimilarToPrevious)
                {
                    _similarToPreviousCnt = 0;
                }
                else
                {
                    _isSimilarToPrevious = false;
                    _similarToPreviousCnt++;
                    if (_similarToPreviousCnt >= 3)
                    {
                        continue;
                    }
                }
                
                for (var i = 0; i < SectionCount; i++)
                {
                    _uniqueSpawnPositions.Push(tempArray[i]);
                    _lastUniqueSpawnPositions[i] = tempArray[i];
                }

                break;
            }
        }
        
        public void HandleObject(XElement element)
        {
            var eId = element.Attribute("id")?.Value;
            
            var objectName = element.Attribute("bundle")?.Value.Split('/').ToList().Last();
            
            var rotation = element.Attribute("rotation")!.Value.Split(',').Select(float.Parse).ToArray();
            var rotationVec = Quaternion.Euler(rotation[0], rotation[1], rotation[2]);
            
            var offset = element.Attribute("offset")!.Value.Split(',').Select(float.Parse).ToArray();
            var offsetVec = new Vector3(offset[0], offset[1], offset[2]);

            var scaleOverride = element.Attribute("scale") != null ? float.Parse(element.Attribute("scale")!.Value) : 1;

            var spawnPoint =  SpawnPoints[_uniqueSpawnPositions.Pop()];

            var obj = prefabs.Find(x => string.Equals(x.name, objectName, StringComparison.CurrentCultureIgnoreCase));
            var go = Instantiate(obj, Vector3.zero, Quaternion.identity);
            
            Bounds bounds;
            if (go.transform.childCount > 0)
            {
                bounds = new Bounds(Vector3.zero, Vector3.zero);
                
                foreach (var rendInChild in go.GetComponentsInChildren<Renderer>())
                {
                    bounds.Encapsulate(rendInChild.bounds);
                }
            }
            else
            {
                bounds = go.GetComponent<Renderer>().bounds;
            }
            
            var minBound = MainCamera.WorldToScreenPoint(bounds.min);
            var maxBound = MainCamera.WorldToScreenPoint(bounds.max);
            var midBound = MainCamera.WorldToScreenPoint(bounds.center);
            
            var division = SectionCount == 2 ? 2f : 3f;

            var scaleX = Screen.width / division / (Math.Abs(maxBound.x - minBound.x));
            var scaleY = Screen.height / division / (Math.Abs(maxBound.y - minBound.y));
            var scale = Math.Min(scaleX, scaleY);

            go.transform.localScale = new Vector3(scaleOverride * scale, scaleOverride * scale, scaleOverride * scale);
            go.transform.Translate(spawnPoint.Pos + offsetVec);
            go.transform.rotation = rotationVec;


            var type = eId switch
            {
                "rewarded" => ObjectType.Reward,
                "punished" => ObjectType.Punish,
                _ => ObjectType.Neutral
            };

            spawnPoint.Window.Type = type;
            go.GetComponent<ObjectController>().Type = type;
        }
        
        public void SaveAndExit()
        {
            _logger.SaveLogsToDisk();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        
    }
}