using UnityEngine;

namespace DefaultNamespace
{
    public class MenuControls : MonoBehaviour
    {
        private Vector2 _fingerDown;
        private Vector2 _fingerUp;
        private float swipeThreshold = (float) Screen.height / 4;

        private void Update()
        {
            if (Input.touchCount != 3) return;
            
            var touch = Input.touches[0];
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    _fingerUp = touch.position;
                    break;
                case TouchPhase.Ended:
                    _fingerDown = touch.position;
                    CheckSwipe();
                    break;
            }
        }

        private void CheckSwipe()
        {
            if (_fingerDown.y - _fingerUp.y < swipeThreshold) return;
            GameManager.Instance.menuCanvas.SetActive(!GameManager.Instance.menuCanvas.activeSelf);
            _fingerUp = _fingerDown;
        }
    }
}