    using UnityEngine;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine.Windows.Speech;
    using System.Linq;

    public class VoiceMovement : MonoBehaviour
    {
        private KeywordRecognizer keywordRecognizer;
        private Dictionary<string, Action> actions = new Dictionary<string, Action>();

        [Header("Movement Settings")]
        public float moveDistance = 1f; // how far the object moves each command
        public float moveDuration = 1f; // how long the movement takes (seconds)

        void Start()
        {
            // Fix: Remove duplicate AudioListeners in scene
            FixAudioListeners();

            // Add commands and link them to methods
            actions.Add("up", Up);
            actions.Add("down", Down);
            actions.Add("left", Left);
            actions.Add("right", Right);

            // Create recognizer for all command words
            keywordRecognizer = new KeywordRecognizer(actions.Keys.ToArray());
            keywordRecognizer.OnPhraseRecognized += RecognizedSpeech;
            keywordRecognizer.Start();

            Debug.Log("Voice recognition started. Commands: up, down, left, right");
        }

        /// <summary>
        /// Fixes the "2 Audio Listeners" error by disabling extra AudioListeners
        /// Only one AudioListener can be active in a Unity scene at a time
        /// </summary>
        private void FixAudioListeners()
        {
            // Find all AudioListeners in the scene
            AudioListener[] listeners = FindObjectsOfType<AudioListener>();

            // If more than one exists, disable all but the first one
            if (listeners.Length > 1)
            {
                Debug.LogWarning("Found " + listeners.Length + " AudioListeners in scene. Disabling extras to fix error...");

                for (int i = 1; i < listeners.Length; i++)
                {
                    listeners[i].enabled = false;
                    Debug.Log("Disabled AudioListener on: " + listeners[i].gameObject.name);
                }

                Debug.Log("AudioListener fix complete. Only 1 AudioListener is now active.");
            }
            else if (listeners.Length == 0)
            {
                Debug.LogError("No AudioListener found in scene! Please add an AudioListener to your Main Camera.");
            }
        }

        private void RecognizedSpeech(PhraseRecognizedEventArgs speech)
        {
            string command = speech.text.ToLower(); // make it case-insensitive
            Debug.Log("Recognized command: " + command);

            if (actions.ContainsKey(command))
            {
                actions[command].Invoke(); // call the corresponding function
            }
            else
            {
                Debug.LogWarning("Command '" + command + "' not recognized");
            }
        }

        // Movement functions (start a coroutine to move smoothly)
        private void Up() => StartCoroutine(MoveOverTime(Vector3.forward));
        private void Down() => StartCoroutine(MoveOverTime(Vector3.back));
        private void Left() => StartCoroutine(MoveOverTime(Vector3.left));
        private void Right() => StartCoroutine(MoveOverTime(Vector3.right));

        // Coroutine that moves smoothly
        private IEnumerator MoveOverTime(Vector3 direction)
        {
            Vector3 startPosition = transform.position;
            Vector3 endPosition = startPosition + direction * moveDistance;
            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                transform.position = Vector3.Lerp(startPosition, endPosition, elapsed / moveDuration);
                elapsed += Time.deltaTime;
                yield return null; // wait for next frame
            }

            transform.position = endPosition; // ensure it ends exactly at target
        }

        // Cleanup on destroy
        private void OnDestroy()
        {
            if (keywordRecognizer != null && keywordRecognizer.IsRunning)
            {
                keywordRecognizer.OnPhraseRecognized -= RecognizedSpeech;
                keywordRecognizer.Stop();
                keywordRecognizer.Dispose();
            }
        }
    }