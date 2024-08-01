using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace DefaultNamespace
{
    public class FeedbackManager : MonoBehaviour
    {
        public static FeedbackManager Instance;
        public bool IsBlinkPhaseOneReward;
        private GameManager GM;
        private TrialManager T;
        private bool _isFeedbackFirstPhase;
        private int _feedbackIssueCount;
        public int _rewardCount;
        public int _TimeOutCount;

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
            T = TrialManager.Instance;
            InitialSetup();
        }

        public void InitialSetup()
        {
            _feedbackIssueCount = 0;
            _rewardCount = 0;
            _TimeOutCount = 0;
            IsBlinkPhaseOneReward = false;
            _isFeedbackFirstPhase = true;
        }

        private void Update()
        {
            switch (GM.ExperimentPhase)
            {
                case ExperimentPhase.Cue:
                    IssueCue();
                    break;
                case ExperimentPhase.HabituationReward:
                    IssueHabituationReward();
                    break;
                case ExperimentPhase.Reward:
                    if (IsBlinkPhaseOneReward)
                    {
                        IssuePhaseOneReward();
                    }
                    else
                    {
                        IssueReward();
                    }
                    break;
                case ExperimentPhase.Timeout:
                    IssueTimeOut();
                    break;
            }
        }

        public void IssueHabituationReward()
        {
            if (!GM.InitialRewardsActive)
            {
                GM.ExperimentPhase = ExperimentPhase.Trial;
                ConnectionHandler.instance.SendRewardAndTimeOutDisable();
                return;
            };

            if (!GM.Timer._started)
            {
                Debug.Log($"Commencing habituation reward #{++_feedbackIssueCount} from " +
                          $"{GM.InitialRewardsCount}");

                GM.AudioSource.PlayOneShot(GM.Reward.AudioClip);
                ConnectionHandler.instance.SendRewardEnable();
                print(GM.NoInputRequired);
                if(GM.NoInputRequired)
                {
                    StartCoroutine(DisableRewardAndTimeOut());
                }
                if (Application.platform.Equals(RuntimePlatform.Android))
                {

                    // if (SC.ArduinoConnected)
                    // {
                    //     SC.SendMessageToArduino($"reward{GM.Reward.ValveOpenDuration}");
                    //     ConnectionHandler.instance.SendRewardEnable();
                    // }
                    // else
                    // {
                    //     Debug.Log("Connection to arduino not established.");
                    // }
                }

                Debug.Log(GM.Reward.Note);

                GM.Timer.Begin(GM.Reward.WaitDuration);
            }

            if (!GM.Timer.IsFinished()) return;

            if (_feedbackIssueCount < GM.InitialRewardsCount) return;

            _feedbackIssueCount = 0;

            GM.ExperimentPhase = ExperimentPhase.Trial;
            if(!GM.NoInputRequired)
                ConnectionHandler.instance.SendRewardAndTimeOutDisable();


        }
        public IEnumerator DisableRewardAndTimeOut()
        {
            yield return new WaitForSeconds(3);
            ConnectionHandler.instance.SendRewardAndTimeOutDisable();
        }
        public void IssueCue()
        {
            if (!GM.CueActive)
            {
                GM.ExperimentPhase = ExperimentPhase.HabituationReward;
                return;
            }

            if (!GM.Timer._started)
            {
                GM.AudioSource.PlayOneShot(GM.Cue.AudioClip);
                ConnectionHandler.instance.SendRewardEnable();

                if (Application.platform.Equals(RuntimePlatform.Android))
                {
                    // if (SC.ArduinoConnected)
                    // {
                    //     SC.SendMessageToArduino($"reward{GM.Reward.ValveOpenDuration}");
                    //     ConnectionHandler.instance.SendRewardEnable();
                    // }
                    // else
                    // {
                    //     Debug.Log("Connection to arduino not established.");
                    // }
                }

                Debug.Log(GM.Cue.Note);

                GM.Timer.Begin(GM.Cue.WaitDuration);
            }

            if (!GM.Timer.IsFinished()) return;

            GM.ExperimentPhase = ExperimentPhase.HabituationReward;
        }

        public void IssueReward()
        {
            if (!GM.Timer._started)
            {
                GM.InputReceived = true;
                GM.TrialSucceeded = true;
                _isFeedbackFirstPhase = true;

                if (GM.RepeatTrial)
                {
                    GM.RepeatTrial = false;
                    Debug.Log("Correction loop finished");
                }
                else
                {
                    _rewardCount++;
                }

                GM.AudioSource.PlayOneShot(GM.Reward.AudioClip);
                ConnectionHandler.instance.SendRewardEnable();
                if(GM.NoInputRequired)
                {
                    StartCoroutine(DisableRewardAndTimeOut());
                }
                if (Application.platform.Equals(RuntimePlatform.Android))
                {
                    // if (SC.ArduinoConnected)
                    // {

                    //     var coefficient = GM.InTwoPhaseBlink ? 2 : 1;

                    //     SC.SendMessageToArduino($"reward{GM.Reward.ValveOpenDuration * coefficient}");
                    //     ConnectionHandler.instance.SendRewardEnable();
                    // }
                    // else
                    // {
                    //     Debug.Log("Connection to arduino not established.");
                    // }
                }

                Debug.Log(GM.Reward.Note);

                if (!GM.NoInputRequired)
                {
                    GM.ClearGameObjects();
                }

                GM.Timer.Begin(GM.NoInputRequired
                    ? GM.Reward.WaitDuration + GM.NoInputWait
                    : GM.Reward.WaitDuration);
            }

            if (GM.NoInputRequired)
            {
                if (_isFeedbackFirstPhase && GM.Timer.TimePassedInSeconds() >= GM.Reward.WaitDuration)
                {
                    GM.ClearGameObjects();
                    _isFeedbackFirstPhase = false;
                }
            }

            if (!GM.Timer.IsFinished()) return;

            T.CurTrialFinished = true;
            if (!GM.FirstTrialSucceeded) GM.FirstTrialSucceeded = true;
            GM.ExperimentPhase = ExperimentPhase.Trial;
            if(!GM.NoInputRequired)
                ConnectionHandler.instance.SendRewardAndTimeOutDisable();
        }



        public void IssuePhaseOneReward()
        {
            if (!GM.Timer._started)
            {
                IssueSimpleReward();
                GM.InputReceived = true;
                GM.Timer.Begin(GM.TwoPhaseBlinkWait);
            }

            if (!GM.Timer.IsFinished()) return;

            if (!GM.IsBlinkPhaseTwoHidden)
            {
                GM.RewardPoint.Window.StartBlinking(GM.BlinkFrequency, GM.BlinkColor);
            }

            GM.InputReceived = false;
            GM.ExperimentPhase = ExperimentPhase.Wait;
        }

        private void IssueSimpleReward()
        {
            GM.AudioSource.PlayOneShot(GM.Reward.AudioClip);

            GM.ClearGameObjects();

            ConnectionHandler.instance.SendRewardEnable();
            if (!Application.platform.Equals(RuntimePlatform.Android)) return;
            // if (SC.ArduinoConnected)
            // {
            //     SC.SendMessageToArduino($"reward{GM.Reward.ValveOpenDuration}");
            //     ConnectionHandler.instance.SendRewardEnable();
            // }
            // else
            // {
            //     Debug.Log("Connection to arduino not established.");
            // }
        }



        public void IssueTimeOut()
        {
            IsBlinkPhaseOneReward = false;
            if (!GM.Timer._started)
            {
                GM.InputReceived = true;
                _isFeedbackFirstPhase = true;

                if (!GM.RepeatTrial)
                {
                    _TimeOutCount++;
                }

                Debug.Log(GM.TimeOut.Note);

                GM.AudioSource.PlayOneShot(GM.TimeOut.AudioClip);

                GM.feedbackCanvas.SetActive(true);

                GM.ClearGameObjects();

                GM.Timer.Begin(GM.TimeOut.WaitDuration);
            }

            if (_isFeedbackFirstPhase && GM.Timer.TimePassedInSeconds() >= GM.TimeOut.BackgroundDuration)
            {
                GM.feedbackCanvas.SetActive(false);
                _isFeedbackFirstPhase = false;
            }

            if (!GM.Timer.IsFinished()) return;

            T.CurTrialFinished = true;

            if (GM.CorrectionLoopActive)
            {
                GM.RepeatTrial = true;
                Debug.Log("Entered Correction Loop");
            }
            GM.ExperimentPhase = ExperimentPhase.Trial;
            ConnectionHandler.instance.SendRewardAndTimeOutDisable();
        }
    }
}