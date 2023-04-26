using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Object = System.Object;
using Random = System.Random;

namespace DefaultNamespace
{
    public class GameManager : MonoBehaviour
    {
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
        
        public SpawnPoint[] SpawnPoints { get; private set; }
        public Random Rand;
        public Camera MainCamera { get; private set; }
        public TMP_Dropdown configDropdown;

        public string ExperimentType;
        
        public bool InputReceived { get; set; }
        public bool PunishOnEmpty { get; private set; }
        public bool CueActive { get; private set; }
        public bool NoInputRequired { get; private set; }
        public bool InitialRewardsActive { get; private set; }
        public bool CorrectionLoopActive { get; private set; }
        public bool FirstTrialSucceeded { get; set; }
        public bool TrialSucceeded { get; set; }
        public bool RepeatTrial { get; set; }
        public bool InTwoPhaseBlink { get; set; }
        public bool DivsActive { get; private set; }
        private bool _isSimilarToPrevious;
        
        public bool AllowVStack { get; private set; }


        public int SectionCount { get; private set; }
        public int InitialRewardsCount { get; private set; }
        private int[] _lastUniqueSpawnPositions;
        private int _similarToPreviousCnt;
        public float NoInputWait { get; private set; }
        public string NoInputAction { get; private set; }
        public string RootFolder { get; private set; }
        
        [SerializeField] private TextMeshProUGUI xmlContentDisplay;
        [SerializeField] public GameObject menuCanvas;
        [SerializeField] private GameObject dualGameCanvas;
        [SerializeField] private GameObject dualPanel;
        [SerializeField] private GameObject quadGameCanvas;
        [SerializeField] private GameObject quadPanel;
        [SerializeField] private Vector2 quadPanelDefaultMinSize;
        [SerializeField] private Vector2 quadPanelDefaultMaxSize;
        [SerializeField] private GameObject quadRowGameCanvas;
        [SerializeField] private GameObject quadRowPanel;
        [SerializeField] private Vector2 quadRowPanelDefaultMinSize;
        [SerializeField] private Vector2 quadRowPanelDefaultMaxSize;
        [SerializeField] public GameObject feedbackCanvas;
        [SerializeField] private Shader bundleShader;
        [SerializeField] public List<GameObject> prefabs;
        [SerializeField] private WindowController[] dualWindows;
        [SerializeField] private WindowController[] quadWindows;
        [SerializeField] private WindowController[] quadRowWindows;
        [SerializeField] private Image[] dividers;
        
        
        public XElement TrialData { get; private set; }
        public FeedbackObject Cue { get; private set; }
        public AudioSource AudioSource { get; private set; }
        public Color DivColor { get; private set; }
        
        private XElement _experimentConfig;
        private string _configContent;
        private GameObject _gameCanvas;
        private float _orthoSize;
        private float _orthoSizeWidth;

        private RectTransform _dualPanelRectTransform;
        private RectTransform _quadPanelRectTransform;
        private RectTransform _quadRowPanelRectTransform;

        private RectTransform _gamePanelRectTransform;

        private float _scWidthQ;
        private float _scHeightQ;
        private float _scWidthQOffset;
        
        // private Vector2 _quadPanelAnchorMin;
        // private Vector2 _quadPanelAnchorMax;
        
        private Logger _logger;

        // New vars
        public static GameManager Instance { get; private set; }
        private TrialManager T;
        private FeedbackManager F;
        private Timer DestroyTimer;
        public Timer Timer { get; private set; }
        public Timer ExperimentTimer { get; private set; }
        public ExperimentPhase ExperimentPhase;
        public FeedbackObject Reward { get; private set; }
        public FeedbackObject Punish { get; private set; }
        
        public List<XElement> TrialEvents { get; private set; }

        private Stack<int> _uniqueSpawnPositions;

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

            Application.targetFrameRate = 20;
        }

        private void Start()
        {
            T = TrialManager.Instance;
            F = FeedbackManager.Instance;
            
            ExperimentPhase = ExperimentPhase.Preprocess;
            
            RootFolder = Application.platform == RuntimePlatform.Android
                ? "/storage/emulated/0/TouchScreen-Trial-Game"
                : Application.persistentDataPath;
            
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
            
            _dualPanelRectTransform = dualPanel.GetComponent<RectTransform>();
            _quadPanelRectTransform = quadPanel.GetComponent<RectTransform>();
            _quadRowPanelRectTransform = quadRowPanel.GetComponent<RectTransform>();

            AudioSource = gameObject.AddComponent<AudioSource>();

            FillDropDownOptions();
            Timer = new Timer();
            DestroyTimer = new Timer();
            ExperimentTimer = new Timer();
            _uniqueSpawnPositions = new Stack<int>();
            _lastUniqueSpawnPositions = new[] { -1, -1, -1, -1 };
            PunishOnEmpty = true;
            InputReceived = false;
            CueActive = false;
            NoInputRequired = false;
            RepeatTrial = false;
            AllowVStack = true;
            InTwoPhaseBlink = false;
            InitialRewardsCount = 0;
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
            quadRowGameCanvas.SetActive(false);

            T.InitialSetup();
            F.InitialSetup();
            
            DestroyTimer.Clear();
            Timer.Clear();

            PunishOnEmpty = true;
            InputReceived = false;
            CueActive = false;
            NoInputRequired = false;
            RepeatTrial = false;
            AllowVStack = true;
            InitialRewardsCount = 0;
            InTwoPhaseBlink = false;
        }

        public void ClearGameObjects()
        {
            var gameObjectsInScene = FindObjectsOfType<ObjectController>();
            foreach (var go in gameObjectsInScene)
            {
                Destroy(go.gameObject);
            }

            var windowsInScene = FindObjectsOfType<WindowController>();
            foreach (var w in windowsInScene)
            {
                w.StopBlinking();
                if (InTwoPhaseBlink && !F.IsBlinkPhaseOneReward && w.Type.Equals(ObjectType.Reward))
                {
                    StartCoroutine(w.BlinkOnce());
                }
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
            var note = Utils.FindElementByName(element, "note");
            var tone = Utils.FindElementByName(element, "tone");
            var valve = Utils.FindElementByName(element, "valve");
            var timer = Utils.FindElementByName(element, "timer");
            
            var noteString = note.Attribute("text")!.Value;
            var toneFrequency = int.Parse(tone.Attribute("frequency")!.Value);
            var toneDuration = float.Parse(tone.Attribute("duration")!.Value);
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
                    case "experiment":
                        ExperimentType = e.Attribute("type")!.Value;
                        break;
                    case "initial-rewards":
                        InitialRewardsActive = bool.Parse(e.Attribute("active")!.Value);
                        if (!InitialRewardsActive) break;
                        InitialRewardsCount = int.Parse(e.Attribute("count")!.Value);
                        break;
                    case "correction-loop":
                        CorrectionLoopActive = bool.Parse(e.Attribute("active")!.Value);
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
                        var size = e.Attribute("size")?.Value;

                        switch (SectionCount)
                        {
                            case 2:
                                _gameCanvas = dualGameCanvas;
                                _gamePanelRectTransform = _dualPanelRectTransform;
                                break;
                            case 4:

                                Vector2 panelDefaultMinSize;
                                Vector2 panelDefaultMaxSize;

                                if (size == "1x4")
                                {
                                    _gameCanvas = quadRowGameCanvas;
                                    _gamePanelRectTransform = _quadRowPanelRectTransform;
                                    panelDefaultMinSize = quadRowPanelDefaultMinSize;
                                    panelDefaultMaxSize = quadRowPanelDefaultMaxSize;
                                }
                                else
                                {
                                    _gameCanvas = quadGameCanvas;
                                    _gamePanelRectTransform = _quadPanelRectTransform;
                                    panelDefaultMinSize = quadPanelDefaultMinSize;
                                    panelDefaultMaxSize = quadPanelDefaultMaxSize;
                                }

                                try
                                {
                                    var top = float.Parse(e.Attribute("top")!.Value);
                                    var bottom = float.Parse(e.Attribute("bottom")!.Value);
                                    var left = float.Parse(e.Attribute("left")!.Value);
                                    var right = float.Parse(e.Attribute("right")!.Value);

                                    _gamePanelRectTransform.anchorMin = new Vector2(left, bottom);
                                    _gamePanelRectTransform.anchorMax = new Vector2(right, top);

                                    _gamePanelRectTransform.offsetMin = new Vector2(5, 0);
                                    _gamePanelRectTransform.offsetMax = new Vector2(5, 10);
                                }
                                catch (Exception exception)
                                {
                                    // No custom size defined
                                    _gamePanelRectTransform.anchorMin = panelDefaultMinSize;
                                    _gamePanelRectTransform.anchorMax = panelDefaultMaxSize;

                                    _gamePanelRectTransform.offsetMin = new Vector2(5, 0);
                                    _gamePanelRectTransform.offsetMax = new Vector2(5, 10);
                                }

                                break;
                        }


                        var camZPos = MainCamera!.transform.position.z;

                        var scHeight = Screen.height;
                        var scWidth = Screen.width;

                        var anchorMax = _gamePanelRectTransform.anchorMax;
                        var anchorMin = _gamePanelRectTransform.anchorMin;

                        // Screen size for the quadrant trial
                        _scHeightQ = scHeight * (anchorMax.y - anchorMin.y);
                        _scWidthQ = scWidth * (anchorMax.x - anchorMin.x);
                        _scWidthQOffset = scWidth * anchorMin.x;

                        switch (SectionCount)
                        {
                            case 2:
                                SpawnPoints = new[]
                                {
                                    new SpawnPoint(MainCamera!.ScreenToWorldPoint
                                        (new Vector3(scWidth * 1 / 4f, scHeight * 1 / 2f, -camZPos)), dualWindows[0]),
                                    new SpawnPoint(MainCamera!.ScreenToWorldPoint
                                        (new Vector3(scWidth * 3 / 4f, scHeight * 1 / 2f, -camZPos)), dualWindows[1])
                                };
                                break;
                                
                            case 4:
                                if (size == "1x4")
                                {
                                    SpawnPoints = new[]
                                    {
                                        new SpawnPoint(MainCamera!.ScreenToWorldPoint
                                        (new Vector3(_scWidthQ * 1 / 8f + _scWidthQOffset, _scHeightQ * 1 / 2f,
                                            -camZPos)), quadRowWindows[0]),
                                        new SpawnPoint(MainCamera!.ScreenToWorldPoint
                                        (new Vector3(_scWidthQ * 3 / 8f + _scWidthQOffset, _scHeightQ * 1 / 2f,
                                            -camZPos)), quadRowWindows[1]),
                                        new SpawnPoint(MainCamera!.ScreenToWorldPoint
                                        (new Vector3(_scWidthQ * 5 / 8f + _scWidthQOffset, _scHeightQ * 1 / 2f,
                                            -camZPos)), quadRowWindows[2]),
                                        new SpawnPoint(MainCamera!.ScreenToWorldPoint
                                        (new Vector3(_scWidthQ * 7 / 8f + _scWidthQOffset, _scHeightQ * 1 / 2f,
                                            -camZPos)), quadRowWindows[3]),
                                    };
                                }
                                else
                                {
                                    SpawnPoints = new[]
                                    {
                                        new SpawnPoint(MainCamera!.ScreenToWorldPoint
                                            (new Vector3(_scWidthQ * 1 / 4f + _scWidthQOffset, _scHeightQ * 3 / 4f,
                                                -camZPos)),
                                            quadWindows[0]),
                                        new SpawnPoint(MainCamera!.ScreenToWorldPoint
                                        (new Vector3(_scWidthQ * 3 / 4f + _scWidthQOffset, _scHeightQ * 3 / 4f,
                                            -camZPos)), quadWindows[1]),
                                        new SpawnPoint(MainCamera!.ScreenToWorldPoint
                                            (new Vector3(_scWidthQ * 1 / 4f + _scWidthQOffset, _scHeightQ * 1 / 4f,
                                                -camZPos)),
                                            quadWindows[2]),
                                        new SpawnPoint(MainCamera!.ScreenToWorldPoint
                                            (new Vector3(_scWidthQ * 3 / 4f + _scWidthQOffset, _scHeightQ * 1 / 4f,
                                                -camZPos)),
                                            quadWindows[3]),
                                    };
                                }
                                
                                break;
                        }

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
                    case "vertical-stack":
                        AllowVStack = bool.Parse(e.Attribute("active")!.Value);
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
            if (!DestroyTimer._started) return;
            if (!DestroyTimer.IsFinished()) return;

            Debug.Log("Termination time passed, saving experiment results");
            _logger.SaveLogsToDisk();
            
            ClearGameObjects();
            ClearScene();
            ExperimentPhase = ExperimentPhase.Preprocess;
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
            
            _uniqueSpawnPositions.Clear();
            
            while (true)
            {
                
                var tempArray = Enumerable.Range(0, SectionCount).OrderBy(_ => Rand.Next()).ToArray();

                if (!AllowVStack)
                {
                    if (
                        tempArray[0] == 0 && tempArray[1] == 2 ||
                        tempArray[0] == 2 && tempArray [1] == 0 ||
                        tempArray[0] == 1 && tempArray[1] == 3 ||
                        tempArray[0] == 3 && tempArray [1] == 1
                        )
                    {
                       continue; 
                    }
                }    
                
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

                // Debug.Log($"{tempArray[0]}, {tempArray[1]}, {tempArray[2]}, {tempArray[3]}");

                break;
            }
            
        }
        
        public void HandleObject(XElement element)
        {
            // GenerateNewPositions();
            
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

            float scaleX, scaleY;

            if (SectionCount == 2)
            {
                scaleX = Screen.width  / 2f / (Math.Abs(maxBound.x - minBound.x));
                scaleY = Screen.height / 2f / (Math.Abs(maxBound.y - minBound.y)); 
            }
            else
            {
                scaleX = _scWidthQ  / 3f / (Math.Abs(maxBound.x - minBound.x));
                scaleY = _scHeightQ / 3f / (Math.Abs(maxBound.y - minBound.y)); 
            }

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


        public void HandleBlink(XElement element)
        {
            try
            {
                InTwoPhaseBlink = bool.Parse(element.Attribute("two-phase")!.Value);
            }
            catch (Exception e)
            {
                InTwoPhaseBlink = false;
            }
            
            var blinkFrequency = float.Parse(element.Attribute("frequency")!.Value);
            var blinkColor = element.Attribute("color")!.Value.Split(',')
                .Select(x => float.Parse(x) / 255).ToArray();
            
            // GenerateNewPositions();
            
            var spawnPoint = SpawnPoints[_uniqueSpawnPositions.Pop()];
            
            spawnPoint.Window.Type = ObjectType.Reward;

            spawnPoint.Window.StartBlinking(blinkFrequency, blinkColor);
        }
        
        
        public void SaveAndExit()
        {
            if (!_logger.LogsSaved) _logger.SaveLogsToDisk();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        
    }
}