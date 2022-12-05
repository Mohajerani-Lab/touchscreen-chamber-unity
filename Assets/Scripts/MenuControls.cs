using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public class MenuControls : MonoBehaviour
    {
        private Vector2 _fingerDown;
        private Vector2 _fingerUp;
        private float swipeThreshold = (float) Screen.height / 5;
        
        private int _keyCount = 0;
        private DateTime _beginTime = DateTime.Now;

        private bool _isThreeFingerSwipe = false;

        private void Update()
        {
            
            if (Input.GetKeyDown(KeyCode.Escape))
                GameManager.Instance.menuCanvas.SetActive(!GameManager.Instance.menuCanvas.activeSelf);
            
            if (Input.touchCount == 0) return;
            
            var touch = Input.touches[0];
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    _fingerUp = touch.position;
                    break;
                case TouchPhase.Moved:
                    _isThreeFingerSwipe = _isThreeFingerSwipe ? _isThreeFingerSwipe : Input.touchCount >= 3;
                    break;
                case TouchPhase.Ended:
                    _fingerDown = touch.position;
                    CheckSwipe();
                    break;
            }

            // if (!Application.platform.Equals(RuntimePlatform.Android)) return;
            
            
            // switch (++_keyCount)
            // {
            //     case 1:
            //         _beginTime = DateTime.Now;
            //         Debug.Log(_beginTime.Millisecond);
            //         break;
            //     case 3:
            //         if ((DateTime.Now - _beginTime).Milliseconds < 1000)
            //         {
            //             Debug.Log(DateTime.Now.Millisecond);
            //             GameManager.Instance.menuCanvas.SetActive(!GameManager.Instance.menuCanvas.activeSelf);
            //         }
            //         _keyCount = 0;
            //         break;
            //     default:
            //         if ((DateTime.Now - _beginTime).Milliseconds > 1000)
            //         {
            //             Debug.Log(DateTime.Now.Millisecond);
            //             _keyCount = 0;
            //         }
            //         break;
            // }
        }

        private void CheckSwipe()
        {
            if (!_isThreeFingerSwipe) return;
            
            if (_fingerDown.y - _fingerUp.y > swipeThreshold)
            {
                GameManager.Instance.menuCanvas.SetActive(!GameManager.Instance.menuCanvas.activeSelf);
                _fingerUp = _fingerDown;
            }

            _isThreeFingerSwipe = false;
        }
    }
}