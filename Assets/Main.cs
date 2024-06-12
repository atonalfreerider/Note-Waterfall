#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Midi;
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
        MidiFile midi = new(midiPath, false);

        string drumPatternPath = Path.Combine(dataDir.FullName, "drum-patterns.json");
        string drumPatternString = File.ReadAllText(drumPatternPath);
        Dictionary<string, List<List<int>>> drumPatterns =
            JsonConvert.DeserializeObject<Dictionary<string, List<List<int>>>>(drumPatternString);

        string instPatternPath = Path.Combine(dataDir.FullName, "inst-patterns.json");
        string instPatternString = File.ReadAllText(instPatternPath);
        Dictionary<string, List<List<int>>> instPatterns =
            JsonConvert.DeserializeObject<Dictionary<string, List<List<int>>>>(instPatternString);

        List<Tuple<long, long>> measures = ExtractMeasures(midi);

        float lastInstZ = 0;
        for (int trackIndex = 0; trackIndex < midi.Tracks; trackIndex++)
        {
            IList<MidiEvent> trackEvents = midi.Events[trackIndex];

            List<List<int>> patterns;
            string instrumentName = "";
            if (trackEvents.FirstOrDefault() is TextEvent textEvent)
            {
                instrumentName = $"{trackIndex}:{textEvent.Text}";
                instPatterns.TryGetValue(instrumentName, out List<List<int>>? instPattern);
                drumPatterns.TryGetValue(instrumentName, out List<List<int>>? drumPattern);
                if (instPattern == null && drumPattern == null) continue;

                patterns = instPattern ?? drumPattern;
            }
            else
            {
                instrumentName = trackEvents.FirstOrDefault().Channel.ToString();
                instPatterns.TryGetValue(instrumentName, out List<List<int>>? instPattern);
                drumPatterns.TryGetValue(instrumentName, out List<List<int>>? drumPattern);
                if (instPattern == null && drumPattern == null) continue;

                patterns = instPattern ?? drumPattern;
            }

            List<NoteOnEvent> noteEvents = trackEvents.OfType<NoteOnEvent>()
                .Where(x => x.Velocity > 0)
                .ToList();

            List<List<NoteOnEvent>> notesByMeasure = new();
            // iterate through measures and extract each pattern
            foreach (Tuple<long, long> startEnd in measures)
            {
                notesByMeasure.Add(NotesInAMeasure(noteEvents, startEnd.Item1, startEnd.Item2));
            }

            GameObject instGO = new GameObject(instrumentName);
            instGO.transform.SetParent(transform, false);
            instGO.transform.Translate(Vector3.forward * lastInstZ);
            int count = 0;
            foreach (List<int> pattern in patterns)
            {
                GameObject patternGO = new GameObject($"Pattern {count}");
                patternGO.transform.SetParent(instGO.transform, false);
                patternGO.transform.Translate(Vector3.right * count * 20);
                foreach (int measureIndex in pattern)
                {
                    NoteOnEvent firstNote = notesByMeasure[measureIndex].FirstOrDefault();

                    GameObject measureGo = new GameObject("Measure " + measureIndex);
                    measureGo.transform.SetParent(patternGO.transform, false);
                    measureGo.transform.Translate(Vector3.up * measureIndex * .01f);
                    foreach (NoteOnEvent noteOnEvent in notesByMeasure[measureIndex])
                    {
                        int noteKey = noteOnEvent.NoteNumber;
                        long onAbsoluteTime = noteOnEvent.AbsoluteTime;
                        long offAbsoluteTime = noteOnEvent.OffEvent.AbsoluteTime;
                        long duration = offAbsoluteTime - onAbsoluteTime;

                        GameObject noteGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        noteGo.name = "Note " + noteKey;
                        noteGo.transform.SetParent(measureGo.transform, false);

                        noteGo.transform.localScale = new Vector3(.2f, .001f, duration * noteScale);
                        noteGo.transform.Translate(Vector3.right * noteKey * .5f);
                        noteGo.transform.Translate(Vector3.forward *
                                                   (onAbsoluteTime - firstNote.AbsoluteTime + duration * .5f) *
                                                   noteScale);

                        if (noteGo.transform.position.z + duration * .5f * noteScale > lastInstZ)
                        {
                            lastInstZ = noteGo.transform.position.z + duration * .5f * noteScale;
                        }
                    }
                }

                count++;
            }
        }
    }

    static List<Tuple<long, long>> ExtractMeasures(MidiFile midiFile)
    {
        // Assume 4/4 time signature if not specified
        int numerator = 4;
        int denominator = 4;

        // Ticks per quarter note
        int ticksPerQuarterNote = midiFile.DeltaTicksPerQuarterNote;

        // List to hold start and end times of each measure in ticks
        List<Tuple<long, long>> measures = new();

        // Iterate through the MIDI events
        foreach (IList<MidiEvent>? track in midiFile.Events)
        {
            int currentMeasure = 0;
            int ticksPerMeasure = ticksPerQuarterNote * numerator * 4 / denominator;

            foreach (MidiEvent midiEvent in track)
            {
                if (midiEvent is TimeSignatureEvent ts)
                {
                    numerator = ts.Numerator;
                    denominator = (int)Math.Pow(2, ts.Denominator);
                    ticksPerMeasure = ticksPerQuarterNote * numerator * 4 / denominator;
                }

                // Check if we reached the end of the measure
                if (midiEvent.AbsoluteTime / ticksPerMeasure > currentMeasure)
                {
                    long measureStartTime = currentMeasure * ticksPerMeasure;
                    long measureEndTime = (currentMeasure + 1) * ticksPerMeasure;
                    measures.Add(new Tuple<long, long>(measureStartTime, measureEndTime));
                    currentMeasure++;
                }
            }
        }

        return measures;
    }

    static List<NoteOnEvent> NotesInAMeasure(List<NoteOnEvent> allNotes, long startTick, long endTick)
    {
        return allNotes.Where(n => n.AbsoluteTime >= startTick && n.AbsoluteTime < endTick).ToList();
    }
}