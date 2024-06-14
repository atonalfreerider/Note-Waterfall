using System;
using System.Collections.Generic;

[Serializable]
public class Pattern
{
    public string TrackName;
    public int TrackNumber;
    public List<Tuple<int, int>> BaseTimingPattern;
    public List<int> MasterPattern;
    public List<List<int>> NoteVariationPatterns;
}