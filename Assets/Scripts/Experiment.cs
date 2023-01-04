using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization.Configuration;
using UnityEngine;

namespace DefaultNamespace
{
    public class Experiment : MonoBehaviour
    {
        private NewGameManager GM;
        private List<XElement> _collectionElements;
        private List<int> _lastUniqueSpawnPositions = new() {-1, -1, -1,-1};
        private List<int> _tempSpawnPositions;
        private bool _isSimilarToPrevious;
        private int _similarToPreviousCnt;
        private int _currentCollectionCount;
        private bool _trialStarted;
        private bool _processingCollection;

        private void Start()
        {
            GM = NewGameManager.Instance;
            _tempSpawnPositions = new List<int>();
            _collectionElements = new List<XElement>();
            _similarToPreviousCnt = 0;
            _isSimilarToPrevious = false;
            _currentCollectionCount = 0;
            _processingCollection = false;
        }

        private void Update()
        {
            switch (GM.ExperimentPhase)
            {
                case ExperimentPhase.Trial:
                    Main();
                    break;
                case ExperimentPhase.Preprocess:
                    break;
                case ExperimentPhase.Setup:
                    break;
                case ExperimentPhase.HabituationReward:
                    break;
                case ExperimentPhase.Reward:
                    break;
                case ExperimentPhase.Punish:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Main()
        {
            foreach (var e in GM.ExperimentData.Elements())
            {
                switch (e.Name.ToString())
                {
                    case "collection":
                        HandleCollection(e);
                        break;
                }
            }
        }

        private void PerformTrial()
        {
            GenerateNewPositions();
            
            foreach (var e in GM.TrialData.Elements())
            {
                switch (e.Name.ToString())
                {
                    case "collection":
                        HandleCollection(e);
                        break;
                }
            }
        }
        
        public void HandleCollection(XElement element)
        {
            if (_processingCollection)
            {
                ProcessCollection();
                return;
            }
            
            _collectionElements.Clear();

            var innerElements = element.Elements().ToArray();
            
            var collectionType = element.Attribute("sample")!.Value;
            
            switch (collectionType)
            {
                case "sequence":
                    foreach (var e in innerElements)
                    {
                        _collectionElements.Add(e);
                    }
                    break;
                case "random":
                    var rndIdx = GM.Rand.Next(innerElements.Count());
                    _collectionElements.Add(innerElements[rndIdx]);
                    break;
                case "loop":
                    var count = int.Parse(element.Attribute("count")!.Value);
                    for (var i = 0; i < count; i++)
                    {
                        _collectionElements.AddRange(innerElements);
                    }
                    break;
            }

        }

        private void ProcessCollection()
        {
            if (_currentCollectionCount < _collectionElements.Count)
            {
                var e = _collectionElements[_currentCollectionCount];
                switch (e.Name.ToString())
                {
                    case "call":
                        GM.ExperimentPhase = ExperimentPhase.Trial;
                        break;
                    case "object":
                        // HandleObject(e);
                        break;
                }
                _currentCollectionCount++;
            }

            else _processingCollection = false;
        }
        
        private void HandleCall(XElement element)
        {
            var target = element.Attribute("id")?.Value;
            
            switch (target)
            {
                case "issue-trial":
                    if (!GM.CurrentTrialEnded) break;
                    
                    GM.CurrentTrialEnded = false;

                    if (GM.NoInputRequired)
                    {
                        if (!GM.ExperimentTimer._started)
                        {
                            GM.ExperimentPhase = GM.NoInputAction switch
                            {
                                "rewarded" => ExperimentPhase.Reward,
                                "punished" => ExperimentPhase.Punish,
                                _ => throw new ArgumentOutOfRangeException()
                            };
                            GM.ExperimentTimer.Begin((int) GM.NoInputWait * 1000);
                        }

                        else if (!GM.ExperimentTimer.IsFinished()) return;

                        GM.ExperimentPhase = ExperimentPhase.Trial;
                    }
                    else
                    {
                        PerformTrial();
                    }
                    break;
            }
        }
        
        

        private void GenerateNewPositions()
        {
            if (GM.RepeatTrial) return;
            
            GM.ResetSpawnWindows();
            
            while (true)
            {
                // _uniqueSpawnPositions.Clear();
                _lastUniqueSpawnPositions.Clear();

                foreach (var sp in _tempSpawnPositions)
                {
                    _lastUniqueSpawnPositions.Add(sp);
                }

                _tempSpawnPositions.Clear();

                for (var i = 0; i < GM.SectionCount; i++)
                {
                    var num = GM.Rand.Next(GM.SectionCount);
                    while (_tempSpawnPositions.Contains(num))
                    {
                        num = GM.Rand.Next(GM.SectionCount);
                    }

                    _tempSpawnPositions.Add(num);
                }

                for (var i = 0; i < 2; i++)
                {
                    if (!_tempSpawnPositions[i].Equals(_lastUniqueSpawnPositions[i])) continue;
                    _isSimilarToPrevious = true;
                    break;
                }

                if (!_isSimilarToPrevious)
                {
                    _similarToPreviousCnt = 0;
                }
                else
                {
                    _similarToPreviousCnt++;
                    if (_similarToPreviousCnt >= 3)
                    {
                        continue;
                    }
                }

                break;
            }
        }
    }
}