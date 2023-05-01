using System.Collections;
using UnityEngine;

namespace SolPlay.FlappyGame.Runtime.Scripts
{
    [ExecuteInEditMode]
    public class CameraShake : MonoBehaviour {

        public static CameraShake instance;

        private Vector3 _originalPos;
        private float _timeAtCurrentFrame;
        private float _timeAtLastFrame;
        private float _fakeDelta;
    
        public float CameraSize = 10.65f;
        public float Minimum = 5;
    
        void Awake()
        {
            instance = this;
        }

        void Update() {
            // Calculate a fake delta time, so we can Shake while game is paused.
            _timeAtCurrentFrame = Time.realtimeSinceStartup;
            _fakeDelta = _timeAtCurrentFrame - _timeAtLastFrame;
            _timeAtLastFrame = _timeAtCurrentFrame; 
            //GetComponent<Camera>().orthographicSize = CameraSize / Screen.width * Screen.height;
            var orthographicSize = 1 / ((CameraSize / 1000) * Screen.width);
        
            GetComponent<Camera>().orthographicSize = Mathf.Max(Minimum, orthographicSize);
        }

        public static void Shake (float duration, float amount) {
            instance._originalPos = instance.gameObject.transform.localPosition;
            instance.StopAllCoroutines();
            instance.StartCoroutine(instance.cShake(duration, amount));
        }

        public IEnumerator cShake (float duration, float amount) {
            float endTime = Time.time + duration;

            while (duration > 0) {
                transform.localPosition = _originalPos + Random.insideUnitSphere * amount;

                duration -= _fakeDelta;

                yield return null;
            }

            transform.localPosition = _originalPos;
        }
    }
}