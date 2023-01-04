using UnityEngine;

namespace DefaultNamespace
{
    public class FeedbackObject
    {
        // For reward
        public FeedbackObject(
            string note,
            int toneFrequency, 
            float toneDuration, 
            float waitDuration, 
            AudioClip audioClip,
            float valveOpenDuration
            )
        {
            ToneFrequency = toneFrequency;
            ToneDuration = toneDuration;
            AudioClip = audioClip;
            WaitDuration = waitDuration;
            Note = note;
            ValveOpenDuration = valveOpenDuration;
        }
        
        // For punish
        public FeedbackObject(
            string note, 
            int toneFrequency, 
            float toneDuration, 
            float waitDuration, 
            AudioClip audioClip,
            Color backgroundColor, 
            float backgroundDuration
            )
        {
            ToneFrequency = toneFrequency;
            ToneDuration = toneDuration;
            AudioClip = audioClip;
            WaitDuration = waitDuration;
            Note = note;
            BackgroundColor = backgroundColor;
            BackgroundDuration = backgroundDuration;
        }

        public int ToneFrequency { get; set; }
        public float ToneDuration { get; set; }
        public AudioClip AudioClip { get; set; }
        public float WaitDuration { get; set; }
        public string Note { get; set; }
        public Color BackgroundColor { get; set; }
        public float BackgroundDuration { get; set; }
        public float ValveOpenDuration { get; set; }
        
        
    }
}