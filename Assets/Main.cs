#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using File = System.IO.File;

public class Main : MonoBehaviour
{
    public string midiDataPath;

    const float noteScale = .01f;

    public void Awake()
    {
        DirectoryInfo dataDir = new(midiDataPath);
        string midiPath = dataDir.GetFiles("*.mid")[0].FullName;

        // Load the MIDI file

        string drumPatternPath = Path.Combine(dataDir.FullName, "drum-patterns.json");
        string drumPatternString = File.ReadAllText(drumPatternPath);
        List<Pattern> drumPatterns = JsonConvert.DeserializeObject<List<Pattern>>(drumPatternString);

        string instPatternPath = Path.Combine(dataDir.FullName, "other-patterns.json");
        string instPatternString = File.ReadAllText(instPatternPath);
        Dictionary<string, List<Pattern>> instPatterns =
            JsonConvert.DeserializeObject<Dictionary<string, List<Pattern>>>(instPatternString);

        foreach ((string track, List<Pattern> patterns) in instPatterns)
        {
            GameObject instGO = new GameObject(track);
            instGO.transform.SetParent(transform, false);
            int count = 0;
            foreach (Pattern pattern in patterns)
            {
                GameObject patternGO = new GameObject($"Pattern {count}");
                patternGO.transform.SetParent(instGO.transform, false);
                patternGO.transform.Translate(Vector3.right * count * 20);
                foreach (Tuple<int, int> baseStartStop in pattern.BaseTimingPattern)
                {
                    long duration = baseStartStop.Item2 - baseStartStop.Item1;

                    GameObject noteGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    
                    noteGo.transform.SetParent(patternGO.transform, false);

                    noteGo.transform.localScale = new Vector3(.2f, .001f, duration * noteScale);
                    noteGo.transform.Translate(Vector3.forward *
                                               (baseStartStop.Item1 + duration * .5f) *
                                               noteScale);
                }

                count++;
            }
        }
    }
}