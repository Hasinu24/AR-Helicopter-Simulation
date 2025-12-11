# AR-Helicopter-Simulation

Repository contents and ready-to-copy files for the Unity AR helicopter voice-control project by Hasinu Ravishka.

---

## Repository structure

```
AR-Helicopter-Simulation/
├── .gitignore
├── LICENSE
├── README.md
├── Assets/
│   ├── Scripts/
│   │   └── VoiceMovement.cs
│   ├── Prefabs/
│   │   └── Helicopter.prefab  (example placeholder)
│   └── Documentation/
│       └── figures/           (images for README and report)
└── ProjectSettings/          (optional: include if you want exact Unity settings)
```

---

## README.md

````markdown
# AR Helicopter Simulation (Voice-Controlled)

**Author:** Hasinu Ravishka  
**Supervisor:** Maryam Banitalebi Dehkordi

## Overview
This repository contains the Unity project and supporting files for a voice-controlled AR helicopter prototype. The system demonstrates a simple, offline voice interface (using Unity's `KeywordRecognizer`) to move an AR helicopter model in four directions: `up`, `down`, `left`, and `right`.

## Features
- Offline keyword-based speech recognition (Unity `KeywordRecognizer`).
- Coroutine-based smooth movement (Vector3.Lerp).
- AR rendering using AR Foundation (project setup required).
- Fix for duplicate AudioListener errors.

## Requirements
- Unity 2022 LTS (recommended)  
- AR Foundation package (matching your target platform)  
- Vuforia or AR Foundation image target setup (if using Vuforia, import Vuforia Engine package)  
- Visual Studio (or other C# editor)
- Windows (for `UnityEngine.Windows.Speech` KeywordRecognizer) if you want native offline speech recognition

## Quick setup
1. Clone this repo:

```bash
git clone https://github.com/Hasinu24/AR-Helicopter-Simulation.git
cd AR-Helicopter-Simulation
````

2. Open the project in Unity (use Unity Hub and select the Unity 2022 LTS editor).
3. In **Edit → Project Settings → Player → Other Settings**, set **Active Input Handling** to **Both**.
4. Install required packages via Package Manager:

   * Input System
   * AR Foundation
   * AR Subsystem (ARCore/ARKit as needed)
   * (Optional) Vuforia Engine if you prefer Vuforia image targets
5. Delete the existing *Main Camera* from the scene and add the AR camera provided by your AR package (e.g., **Vuforia → AR Camera** or AR Foundation's AR Camera setup).
6. Add an Image Target (or AR Anchor) and attach the helicopter prefab as a child of the Image Target.
7. Add the script `VoiceMovement.cs` to the helicopter GameObject (Assets/Scripts).
8. Run the scene; on Windows the KeywordRecognizer will start listening for: `up`, `down`, `left`, `right`.

## Files of interest

* `Assets/Scripts/VoiceMovement.cs` — main behaviour script (speech recognition, movement coroutines, audio listener fix).
* `Assets/Prefabs/Helicopter.prefab` — placeholder for the helicopter model (replace with your asset store model).

## Testing notes

* Test in a controlled indoor environment first.
* Expect reduced accuracy in high background noise.
* If running on a non-Windows platform, consider replacing `UnityEngine.Windows.Speech` with a cross-platform speech solution (online or local models).

## Improvements & Ideas

* Add additional commands (stop, rotate, hover, speed up/down).
* Replace keyword recognizer with NLP pipeline for complex commands.
* Add multimodal input (gesture + voice).
* Integrate with MAVLink for real UAV control.



## Contact

Hasinu Ravishka — [https://github.com/Hasinu24/AR-Helicopter-Simulation](https://github.com/Hasinu24/AR-Helicopter-Simulation)

```
```

---

## .gitignore (Unity basics)

```text
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
UserSettings/
.DS_Store
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db
sysinfo.txt
*.apk
*.aab
*.unitypackage
```

---


## Assets/Scripts/VoiceMovement.cs

```csharp
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

    private void FixAudioListeners()
    {
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();

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

    private void Up() => StartCoroutine(MoveOverTime(Vector3.forward));
    private void Down() => StartCoroutine(MoveOverTime(Vector3.back));
    private void Left() => StartCoroutine(MoveOverTime(Vector3.left));
    private void Right() => StartCoroutine(MoveOverTime(Vector3.right));

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
```

---

## Notes

* Replace the placeholder `Helicopter.prefab` with your downloaded 3D model.

* Remove `ProjectSettings/` from tracking if you want per-developer settings to remain local (or include it to reproduce exact Unity configuration).

---
