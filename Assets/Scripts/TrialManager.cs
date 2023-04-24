using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace DefaultNamespace
{
    public class TrialManager : MonoBehaviour
    {
        public static TrialManager Instance { get; private set; }
        public bool CurTrialFinished { get; set; }
        private GameManager GM;
        private FeedbackManager FM;
        private Logger _logger;
        private bool _curTrialStarted;
        private int _curTrialNumber;
        private int _curCorrectionLoopNumber;

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
            GM = GameManager.Instance;
            FM = FeedbackManager.Instance;
            _logger = GetComponent<Logger>();
            InitialSetup();
        }

        public void InitialSetup()
        {
            _curTrialStarted = false;
            CurTrialFinished = false;
            _curTrialNumber = 0;
            _curCorrectionLoopNumber = 0;
        }

        private void Update()
        {
            if (!GM.ExperimentPhase.Equals(ExperimentPhase.Trial)) return;
            
            if (_curTrialNumber >= GM.TrialEvents.Count)
            {
                Debug.Log($"Total Rewarded: {FM._rewardedCount}");
                Debug.Log($"Total Punished: {FM._punishedCount}");
                _logger.SaveLogsToDisk();
                GM.ExperimentPhase = ExperimentPhase.Preprocess;
                return;
            }
            
            if (!_curTrialStarted)
            {
                GM.InputReceived = false;
                _curTrialStarted = true;

                Debug.Log(GM.RepeatTrial
                    ? $"Commencing Correction Loop #{++_curCorrectionLoopNumber}"
                    : $"Commencing trial #{_curTrialNumber + 1} from {GM.TrialEvents.Count}");

                GM.GenerateNewPositions();

                foreach (var element in GM.TrialData.Elements())
                {
                    if (!element.Name.ToString().Equals("collection")) continue;
                    
                    var collection = GM.HandleCollection(element);
                    
                    foreach (var cElement in collection)
                    {
                        switch (cElement.Name.ToString())
                        {
                            case "object":
                                GM.HandleObject(cElement);
                                break;
                            case "blink":
                                GM.HandleBlink(cElement);
                                break;
                        }
                    }
                }

                if (GM.NoInputRequired)
                {
                    GM.ExperimentPhase = ExperimentPhase.Reward;
                }
            }

            if (!CurTrialFinished) return;
            
            _curTrialStarted = false;
            CurTrialFinished = false;

            if (GM.RepeatTrial) return;
            _curTrialNumber++;
            _curCorrectionLoopNumber = 0;
        }
    }
}