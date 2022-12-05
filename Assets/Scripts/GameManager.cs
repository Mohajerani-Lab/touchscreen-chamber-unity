using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Image = UnityEngine.UI.Image;
using Math = System.Math;
using Random = System.Random;

namespace DefaultNamespace
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public bool currentTrialEnded = false;
        public bool experimentStarted = false;

        public bool PunishOnEmpty { get; private set; } = true;
        public bool InputReceived { get; set; } = false;
        
        private bool _correctionLoopActive;
        private bool _initialRewardsActive;
        private int _initialRewardsCount = 0;

        private bool _cueActive;
        private bool _cueGiven = false;
        public bool NoInputRequired { get; private set; } = false;
        public string NoInputAction { get; private set; }
        public float NoInputWait { get; private set; }
        
        public int SectionCount { get; private set; }
        public Camera mainCamera;
        public TMP_Dropdown configDropdown;


        public string RootFolder
        {
            get => rootFolder;
            private set => rootFolder = value;
        }
        // public float ObjectRotationSpeed => objectRotationSpeed;
        public class Feedback
        {
            public int ToneFrequency { get; set; }
            public float ToneDuration { get; set; }
            public AudioClip AudioClip { get; set; }
            public float WaitDuration { get; set; }
            public string Note { get; set; }
            public Color BackgroundColor { get; set; }
            public float BackgroundDuration { get; set; }
            public float ValveOpenDuration { get; set; }
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

        // [SerializeField] [Range(1f, 50f)] private float objectRotationSpeed = 5f;
        [SerializeField] private string rootFolder;
        [SerializeField] private TextMeshProUGUI xmlContentDisplay;
        [SerializeField] public GameObject menuCanvas;
        [SerializeField] private GameObject dualGameCanvas;
        [SerializeField] private GameObject quadGameCanvas;
        [SerializeField] private GameObject feedbackCanvas;
        [SerializeField] private Shader bundleShader;
        [SerializeField] private List<GameObject> prefabs;
        [SerializeField] private WindowController[] quadWindows;
        [SerializeField] private WindowController[] dualWindows;
        
        private SpawnPoint[] _spawnPoints;
        private Stack<SpawnPoint> _uniqueSpawnPoints;
        private string _configContent;
        private Random _random;
        private XElement _experimentConfig;
        private XElement _trial;
        private bool _firstTrialSucceeded;
        private Feedback _cue;
        private AudioSource _audioSource;

        private GameObject _gameCanvas;

        public Feedback Reward { get; private set; }

        public Feedback Punish { get; private set; }
        
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
            // _shaderToReplace = Shader.Find("Standard");
            _random = new Random();

            mainCamera = Camera.main;
            _orthoSize = mainCamera!.orthographicSize * 2;

            _audioSource = gameObject.AddComponent<AudioSource>();

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

        private void ResetSpawnPoints()
        {
            foreach (var spawnPoint in _spawnPoints)
            {
                spawnPoint.Window.Type = ObjectType.Neutral;
            }
        }

        private void ClearScene()
        {
            prefabs.Clear();
                        
            dualGameCanvas.SetActive(false);
            quadGameCanvas.SetActive(false);
            
            var sceneObjects = FindObjectsOfType<MonoBehaviour>();
            foreach (var so in sceneObjects)
            {
                so.StopAllCoroutines();
            }
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
            StartCoroutine(ParseConfig(_configContent));
        }

        private void SetupCanvas()
        {
            menuCanvas.SetActive(false);
            _gameCanvas.SetActive(true);
            
            foreach (var divider in
                     _gameCanvas.GetComponentsInChildren<Image>().ToList().FindAll
                         (x => x.GetComponent<WindowController>() is null))
            {
                if (DivsActive)
                {
                    divider.color = DivColor;
                }
                else
                {
                    divider.gameObject.SetActive(false);
                }
            }
        }

        private IEnumerator PrepareScene(XElement element, XElement cfg)
        {
            var initialRewardsElem = FindElementByName(element, "initial-rewards");
            var correctionLoopElem = FindElementByName(element, "correction-loop");
            var punishOnEmptyElem  = FindElementByName(element, "punish-on-empty");
            var noInputElem        = FindElementByName(element, "no-input");
            var sectionsElem       = FindElementByName(element, "sections");
            var dividersElem       = FindElementByName(element, "dividers");
            var sprite     = FindElementByName(element, "sprite");
            
            _initialRewardsActive = bool.Parse(initialRewardsElem.Attribute("active")!.Value);
            if (_initialRewardsActive)
            {
                _initialRewardsCount = int.Parse(initialRewardsElem.Attribute("count")!.Value);
            }
            
            _correctionLoopActive = bool.Parse(correctionLoopElem.Attribute("active")!.Value);
            
            PunishOnEmpty = bool.Parse(punishOnEmptyElem.Attribute("active")!.Value);

            NoInputRequired = bool.Parse(noInputElem.Attribute("active")!.Value);
            if (NoInputRequired)
            {
                NoInputAction = noInputElem.Attribute("action")!.Value;
                NoInputWait = float.Parse(noInputElem.Attribute("wait-between")!.Value);
            }
            
            SectionCount = int.Parse(sectionsElem.Attribute("count")!.Value);
            
            _gameCanvas = SectionCount == 2 ? dualGameCanvas : quadGameCanvas;
            
            DivsActive = bool.Parse(dividersElem.Attribute("active")!.Value);
            if (DivsActive)
            {
                var colorString = dividersElem.Attribute("color")!.Value.Split(',').Select(x => float.Parse(x) / 255).ToArray();
                DivColor = new Color(colorString[0], colorString[1], colorString[2]);
            }

            var spriteColor = sprite.Attribute("color")!.Value.Split(',').Select(x => float.Parse(x) / 255).ToArray();
            mainCamera.backgroundColor = new Color(spriteColor[0], spriteColor[1], spriteColor[2]);
            
            var camZPos = mainCamera!.transform.position.z;
            var scHeight = Screen.height;
            var scWidth = Screen.width;

            _spawnPoints = SectionCount == 4 ? new []
            {
                new SpawnPoint(mainCamera!.ScreenToWorldPoint(new Vector3(scWidth / 4f, scHeight * 3/4f, -camZPos)), quadWindows[0]),
                new SpawnPoint(mainCamera!.ScreenToWorldPoint(new Vector3(scWidth * 3/4f, scHeight * 3/4f, -camZPos)), quadWindows[1]),
                new SpawnPoint(mainCamera!.ScreenToWorldPoint(new Vector3(scWidth / 4f, scHeight / 4f, -camZPos)), quadWindows[2]),
                new SpawnPoint(mainCamera!.ScreenToWorldPoint(new Vector3(scWidth * 3/4f, scHeight / 4f, -camZPos)), quadWindows[3]),
            } :
            new []
            {
                new SpawnPoint(mainCamera!.ScreenToWorldPoint(new Vector3(scWidth / 4f, scHeight / 2f, -camZPos)), dualWindows[0]),
                new SpawnPoint(mainCamera!.ScreenToWorldPoint(new Vector3(scWidth * 3/4f, scHeight / 2f, -camZPos)), dualWindows[1]) 
            };

            foreach (var e in element.Elements())
            {
                switch (e.Name.ToString())
                {
                    case "load":
                        yield return StartCoroutine(HandleLoad(e, cfg));
                        break;
                    case "timer":
                        StartCoroutine(HandleTimer(e, cfg));
                        break;
                }
            }
            
        }

        private IEnumerator Main(XElement element, XElement cfg)
        {
            foreach (var e in element.Elements())
            {
                    Func<XElement, XElement, IEnumerator> executor = e.Name.ToString() switch
                {
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

            prefab = ProcessObject(prefab);
            
            prefabs.Add(prefab);
            
            yield return 0;
        }

        private GameObject ProcessObject(GameObject obj)
        {
            if (obj.transform.childCount == 0)
            {
                obj.AddComponent<ObjectController>();

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

            obj.AddComponent<ObjectController>();
            
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
        

        private GameObject ProcessObjectWithChildren(GameObject obj)
        {
            
            
            var meshesInChildren = obj.GetComponentsInChildren<MeshFilter>();
            
            var x = 0f;
            var y = 0f;
            var z = 0f;
            foreach (var meshFilter in meshesInChildren)
            {
                var center = meshFilter.sharedMesh.bounds.center;
                x += center.x;
                y += center.y;
                z += center.z;
            }
            
            var colliderCenter = new Vector3(x/meshesInChildren.Length, y/meshesInChildren.Length, z/meshesInChildren.Length);
            
            // colliderCenter = colliderCenter / meshesInChildren.Length;
            
            var colliderBounds = new Bounds(colliderCenter, Vector3.zero);
            
            foreach (var meshFilter in meshesInChildren)
            {
                Debug.Log(meshFilter.gameObject.name);
                if (!colliderBounds.Contains(meshFilter.sharedMesh.bounds.center))
                    colliderBounds.Encapsulate(meshFilter.sharedMesh.bounds);
            }

            var boxCollider = obj.AddComponent<BoxCollider>();
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
                Debug.Log("Termination time has passed. Experiment ended.");
                SaveAndExit();
            }
            yield return 0;
        }

        public IEnumerator IssueReward(Feedback feedback)
        {
            InputReceived = true;

            _audioSource.PlayOneShot(feedback.AudioClip);
            
            if (Application.platform.Equals(RuntimePlatform.Android))
            {
                if (SerialComs.Instance.ArduinoConnected)
                {
                    SerialComs.Instance.SendMessageToArduino($"reward{feedback.ValveOpenDuration}");
                }
                else
                {
                    Debug.Log("Connection to arduino not established.");
                }
            }
            
            Debug.Log(feedback.Note);

            if (!NoInputRequired)
            {
                ClearGameObjects();
            }
            
            yield return new WaitForSeconds(feedback.WaitDuration);
            
            if (NoInputRequired)
            {
                ClearGameObjects();
            }
            
            currentTrialEnded = true;
            if (!_firstTrialSucceeded) _firstTrialSucceeded = true;
        }

        public IEnumerator IssuePunish(Feedback feedback)
        {
            InputReceived = true;
            
            Debug.Log(feedback.Note);

            _audioSource.PlayOneShot(feedback.AudioClip);
            
            feedbackCanvas.SetActive(true);
            
            ClearGameObjects();

            yield return new WaitForSeconds(feedback.BackgroundDuration);
            
            feedbackCanvas.SetActive(false);

            yield return new WaitForSeconds(feedback.WaitDuration);
            
            currentTrialEnded = true;
        }

        private void SetupReward(XElement element, XElement cfg)
        {
            Reward = new Feedback();
            
            var tone = FindElementByName(element, "tone");
            var note = FindElementByName(element, "note");
            var timer = FindElementByName(element, "timer");
            var valve = FindElementByName(element, "valve");
            
            
            Reward.ToneFrequency = int.Parse(tone.Attribute("frequency")!.Value);
            Reward.ToneDuration = float.Parse(tone.Attribute("duration")!.Value);
            Reward.Note = note.Attribute("text")!.Value;
            Reward.WaitDuration = float.Parse(timer.Attribute("duration")!.Value);
            Reward.ValveOpenDuration = float.Parse(valve.Attribute("duration")!.Value);
            
            var position = 0;
            var freq  = 44100;
            
            var clipLen = freq * Reward.ToneDuration;
            
            var clip = AudioClip.Create("RewardTone", (int) clipLen, 1, freq ,
                true, 
                data =>
                {
                    var count = 0;
                    while (count < data.Length)
                    {
                        data[count] = Mathf.Sin(2 * Mathf.PI * Reward.ToneFrequency * position / freq );
                        position++;
                        count++;
                    }
                }, newPosition => position = newPosition);

            // Reward.ToneAudio = gameObject.AddComponent<AudioSource>();
            Reward.AudioClip = clip;
        }
        
        private void SetupCue(XElement element, XElement cfg)
        {
            _cueActive = true;
            _cue = new Feedback();

            var tone = FindElementByName(element, "tone");
            var note = FindElementByName(element, "note");
            var timer = FindElementByName(element, "timer");
            var valve = FindElementByName(element, "valve");
            
            _cue.ToneFrequency = int.Parse(tone.Attribute("frequency")!.Value);
            _cue.ToneDuration = float.Parse(tone.Attribute("duration")!.Value);
            _cue.Note = note.Attribute("text")!.Value;
            _cue.WaitDuration = float.Parse(timer.Attribute("duration")!.Value);
            _cue.ValveOpenDuration = float.Parse(valve.Attribute("duration")!.Value);
            
            var position = 0;
            var freq  = 44100;
            
            var clipLen = freq * _cue.ToneDuration;
            
            var clip = AudioClip.Create("RewardTone", (int) clipLen, 1, freq,
                true, 
                data =>
                {
                    var count = 0;
                    while (count < data.Length)
                    {
                        data[count] = Mathf.Sin(2 * Mathf.PI * Reward.ToneFrequency * position / freq);
                        position++;
                        count++;
                    }
                }, newPosition => position = newPosition);

            // _cue.ToneAudio = gameObject.AddComponent<AudioSource>();
            _cue.AudioClip = clip;
        }

        private void SetupPunish(XElement element, XElement cfg)
        {
            Punish = new Feedback();

            var tone = FindElementByName(element, "tone");
            var note = FindElementByName(element, "note");
            var timer = FindElementByName(element, "timer");
            var sprite = FindElementByName(element, "sprite");
            
            Punish.ToneFrequency = int.Parse(tone.Attribute("frequency")!.Value);
            Punish.ToneDuration = float.Parse(tone.Attribute("duration")!.Value);
            Punish.Note = note.Attribute("text")!.Value;
            Punish.WaitDuration = float.Parse(timer.Attribute("duration")!.Value);
            var spriteColor = sprite.Attribute("color")!.Value.Split(',').Select(x => float.Parse(x) / 255).ToArray();
            Punish.BackgroundColor = new Color(spriteColor[0], spriteColor[1], spriteColor[2]);
            feedbackCanvas.GetComponentInChildren<Image>().color = Punish.BackgroundColor;
            Punish.BackgroundDuration = float.Parse(sprite.Attribute("duration")!.Value);
            
            var position = 0;
            var freq  = 44100;
            
            var clipLen = freq * Punish.ToneDuration;

            var clip = AudioClip.Create("PunishTone", (int) clipLen, 1, freq,
                true, 
                data =>
            {
                var count = 0;
                while (count < data.Length)
                {
                    data[count] = Mathf.Sin(2 * Mathf.PI * Punish.ToneFrequency * position / freq);
                    position++;
                    count++;
                }
            }, newPosition => position = newPosition);

            Punish.AudioClip = clip;
        }

        private IEnumerator PerformTrial()
        {
            ResetSpawnPoints();
            _uniqueSpawnPoints = new Stack<SpawnPoint>(_spawnPoints.OrderBy(_ => _random.Next()));

            if (_cueActive && !_cueGiven)
            {
                yield return StartCoroutine(IssueReward(_cue));
                _cueGiven = true;
            }
            
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
            
            var rotation = element.Attribute("rotation")!.Value.Split(',').Select(float.Parse).ToArray();
            var rotationVec = Quaternion.Euler(rotation[0], rotation[1], rotation[2]);
            
            var offset = element.Attribute("offset")!.Value.Split(',').Select(float.Parse).ToArray();
            var offsetVec = new Vector3(offset[0], offset[1], offset[2]);

            var scaleOverride = element.Attribute("scale") != null ? float.Parse(element.Attribute("scale")!.Value) : 1;

            var spawnPoint =  _uniqueSpawnPoints.Pop();

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
            
            var minBound = mainCamera.WorldToScreenPoint(bounds.min);
            var maxBound = mainCamera.WorldToScreenPoint(bounds.max);
            var midBound = mainCamera.WorldToScreenPoint(bounds.center);
            
            var division = GameManager.Instance.SectionCount == 2 ? 2f : 3f;

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
                    Debug.Log("Commencing Trial");
                    if (NoInputRequired)
                    {
                        yield return StartCoroutine(executor(e, cfg));
                        yield return StartCoroutine(NoInputAction switch
                        {
                            "rewarded" => IssueReward(Reward),
                            "punished" => IssuePunish(Punish),
                            _ => throw new ArgumentOutOfRangeException()
                        });
                        yield return new WaitForSeconds(NoInputWait);
                    }
                    else
                    {
                        currentTrialEnded = false;
                        InputReceived = false;
                        StartCoroutine(executor(e, cfg));
                        yield return new WaitUntil((() => currentTrialEnded));
                    }
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
                InputReceived = false;
                Debug.Log($"Commencing correction loop #{++count}");
                yield return StartCoroutine(PerformTrial());
                yield return new WaitUntil(() => currentTrialEnded);
            }

            Debug.Log("Correction loop finished");
        }
        

        private IEnumerator DoInitialRewards()
        {
            if (_initialRewardsActive)
            {
                for (var i = 0; i < _initialRewardsCount; i++)
                {
                    Debug.Log($"Commencing habituation reward #{i+1} from {_initialRewardsCount}");
                    yield return StartCoroutine(IssueReward(Reward));
                }

                Debug.Log("Habituation rewards finished"); 
            }

            if (_correctionLoopActive) yield return StartCoroutine(DoCorrectionLoop());
        }

        private IEnumerator ParseConfig(string xmlContent)
        {
            _experimentConfig = XElement.Parse(xmlContent);

            var rewardCfg = _experimentConfig.Elements().Where(
                e => e.Name.ToString().Equals("function") &&
                     e.Attribute("id")!.Value == "rewarded").ToArray();
            
            if (rewardCfg.Length > 0)
            {
                SetupReward(rewardCfg[0], _experimentConfig);
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
                _cueActive = true;
                SetupCue(cueCfg[0], _experimentConfig);
            }

            var trialCfg = _experimentConfig.Elements().Where(
                e => e.Name.ToString().Equals("function") &&
                     e.Attribute("id")!.Value == "trial").ToArray();

            _trial = trialCfg[0];
            
            var prepFunc = _experimentConfig.Elements().Where(
                e => e.Name.ToString().Equals("function") &&
                     e.Attribute("id")!.Value == "prepare").ToArray();
            
            yield return StartCoroutine(PrepareScene(prepFunc[0], _experimentConfig));

            
            SetupCanvas();

            yield return StartCoroutine(DoInitialRewards());

            var mainFunc = _experimentConfig.Elements().Where(
                e => e.Name.ToString().Equals("function") &&
                     e.Attribute("id")!.Value == "main").ToArray();
            
            yield return StartCoroutine(Main(mainFunc[0], _experimentConfig));
            
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

        private XElement FindElementByName(XElement parent, string childName)
        {
            return parent.Elements().Where(e => e.Name.ToString().Equals(childName)).ToArray()[0];
        }
    }
}