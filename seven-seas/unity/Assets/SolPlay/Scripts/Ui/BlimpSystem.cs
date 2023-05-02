using System;
using System.Collections;
using Frictionless;
using UnityEngine;

namespace SolPlay.Scripts.Ui
{
    public class BlimpSystem : MonoBehaviour
    {
        public enum BlimpType
        {
            TextWithBackground,
            Boost,
            Score
        }

        public class ShowLogMessage
        {
            public string BlimpText;
            public BlimpType BlimpType;

            public ShowLogMessage(string blimpText, BlimpType blimpType = BlimpType.TextWithBackground)
            {
                BlimpText = blimpText;
                BlimpType = blimpType;
            }
        }

        public class ShowBlimpMessage
        {
            public string BlimpText;
            public BlimpType BlimpType;
            public Vector3 Position;

            public ShowBlimpMessage(string blimpText, Vector3 position, BlimpType blimpType = BlimpType.TextWithBackground)
            {
                BlimpText = blimpText;
                BlimpType = blimpType;
                Position = position;
            }
        }

        public TextBlimp TextBlimpPrefab;
        public TextBlimp BoostBlimpPrefab;
        public TextBlimp ScoreBlimpPrefab;
        public GameObject LogMessageRoot;
        public GameObject BlimpRoot;

        // Start is called before the first frame update
        void Start()
        {
            MessageRouter.AddHandler<ShowLogMessage>(OnShowLogMessage);
            MessageRouter.AddHandler<ShowBlimpMessage>(OnShowBlimpMessage);
            Application.logMessageReceived += OnLogMessage;
        }

        private void OnShowBlimpMessage(ShowBlimpMessage message)
        {
            // TODO: Pool for production
            TextBlimp blimpPrefab = null;
            string animationId = null;
            switch (message.BlimpType)
            {
                case BlimpType.TextWithBackground:
                    blimpPrefab = TextBlimpPrefab;
                    break;
                case BlimpType.Boost:
                    blimpPrefab = BoostBlimpPrefab;
                    animationId = "BoostBlimpAppear";
                    break;
                case BlimpType.Score:
                    blimpPrefab = ScoreBlimpPrefab;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(message), message, null);
            }

            var instance = Instantiate(blimpPrefab, BlimpRoot.transform);
            instance.transform.position = message.Position;
            instance.SetData(message.BlimpText);
            if (!string.IsNullOrEmpty(animationId))
            {
                instance.GetComponent<Animator>().Play(animationId);
            }

            StartCoroutine(DestroyDelayed(instance.gameObject));
        }

        private void OnDestroy()
        {
            MessageRouter.RemoveHandler<ShowLogMessage>(OnShowLogMessage);
            Application.logMessageReceived -= OnLogMessage;
        }

        private void OnLogMessage(string condition, string stacktrace, LogType type)
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (type == LogType.Error || type == LogType.Exception)
            {
                SpawnBlimp(condition, BlimpType.TextWithBackground);
                Debug.Log("Error" + stacktrace);
            }
        }

        private void OnShowLogMessage(ShowLogMessage message)
        {
            SpawnBlimp(message.BlimpText, message.BlimpType);
        }

        private void SpawnBlimp(string message, BlimpType blimpType)
        {
            // TODO: Pool for production
            TextBlimp blimpPrefab = null;
            string animationId = null;
            switch (blimpType)
            {
                case BlimpType.TextWithBackground:
                    blimpPrefab = TextBlimpPrefab;
                    break;
                case BlimpType.Boost:
                    blimpPrefab = BoostBlimpPrefab;
                    animationId = "BoostBlimpAppear";
                    break;
                case BlimpType.Score:
                    blimpPrefab = ScoreBlimpPrefab;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(blimpType), blimpType, null);
            }

            var instance = Instantiate(blimpPrefab, LogMessageRoot.transform);
            instance.SetData(message);
            if (!string.IsNullOrEmpty(animationId))
            {
                instance.GetComponent<Animator>().Play(animationId);
            }

            StartCoroutine(DestroyDelayed(instance.gameObject));
        }

        private IEnumerator DestroyDelayed(GameObject go)
        {
            yield return new WaitForSeconds(2.5f);
            Destroy(go);
        }
    }
}