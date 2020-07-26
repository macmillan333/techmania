﻿using System;
using System.Collections.Generic;

// Track is the container of all patterns in a musical track. In anticipation of
// format updates, each format version is a derived class of TrackBase.
//
// Because class names are not serialized, we can change class names
// however we want without breaking old files, so the current version
// class will always be called "Track", and deprecated versions will be
// called "TrackVersion1" or such.

[Serializable]
public class TrackBase
{
    public string version;

    private string Serialize()
    {
        return UnityEngine.JsonUtility.ToJson(this, prettyPrint: true);
    }
    private static TrackBase Deserialize(string json)
    {
        string version = UnityEngine.JsonUtility.FromJson<TrackBase>(json).version;
        switch (version)
        {
            case Track.kVersion:
                return UnityEngine.JsonUtility.FromJson<Track>(json);
                // For non-current versions, maybe attempt conversion?
            default:
                throw new Exception($"Unknown version: {version}");
        }
    }

    public TrackBase Clone()
    {
        return Deserialize(Serialize());
    }

    public void SaveToFile(string path)
    {
        System.IO.File.WriteAllText(path, Serialize());
    }

    public static TrackBase LoadFromFile(string path)
    {
        string fileContent = System.IO.File.ReadAllText(path);
        return Deserialize(fileContent);
    }
}

// Heavily inspired by bmson:
// https://bmson-spec.readthedocs.io/en/master/doc/index.html#format-overview
[Serializable]
public class Track : TrackBase
{
    public const string kVersion = "1";
    public Track() { version = kVersion; }
    public Track(string title, string artist)
    {
        version = kVersion;
        trackMetadata = new TrackMetadata();
        trackMetadata.title = title;
        trackMetadata.artist = artist;
        patterns = new List<Pattern>();
    }

    public TrackMetadata trackMetadata;
    public List<Pattern> patterns;
}

[Serializable]
public class TrackMetadata
{
    // Text stuff.

    public string title;
    public string subtitle;
    public string artist;
    public List<string> subArtists;
    public string genre;

    // In track select screen.

    // Filename of eyecatch image.
    public string eyecatchImage;
    // Filename of preview music.
    public string previewTrack;
    // In seconds.
    public double previewStartTime;
    public double previewEndTime;

    // In gameplay.

    // Filename of background image, used in loading screen
    public string backImage;
    // Filename of background animation (BGA)
    // If empty, will show background image
    public string bga;
    // Play BGA from this time.
    public double bgaStartTime;
}

[Serializable]
public class Pattern
{
    public PatternMetadata patternMetadata;
    public List<BpmEvent> bpmEvents;
    public List<SoundChannel> soundChannels;

    public const int pulsesPerBeat = 240;

    // Filled at runtime, sorted and indexed by pulse.
    [NonSerialized]
    public List<List<Note>> sortedNotes;

    public void FillUnserializedFields()
    {
        sortedNotes = new List<List<Note>>();
        foreach (SoundChannel channel in soundChannels)
        {
            foreach (Note n in channel.notes)
            {
                n.sound = channel.name;
                AddToSortedNotes(n);
            }
        }
    }

    // Assumes no note exists at the same location.
    public void AddNote(Note n)
    {
        // Write to serialized fields.
        if (soundChannels == null)
        {
            soundChannels = new List<SoundChannel>();
        }
        SoundChannel channel = soundChannels.Find(
            (SoundChannel c) => { return c.name == n.sound; });
        if (channel == null)
        {
            channel = new SoundChannel();
            channel.name = n.sound;
            channel.notes = new List<Note>();
            soundChannels.Add(channel);
        }
        channel.notes.Add(n);

        // Write to unserialized fields.
        AddToSortedNotes(n);
    }

    // This does not check for notes crossing each other,
    // such as a basic note at the middle of a hold note.
    public bool HasNoteAt(int pulse, int lane)
    {
        if (sortedNotes.Count < pulse + 1) return false;
        if (sortedNotes[pulse] == null) return false;
        foreach (Note n in sortedNotes[pulse])
        {
            if (n.lane == lane) return true;
        }
        return false;
    }

    private void AddToSortedNotes(Note n)
    {
        while (sortedNotes.Count < n.pulse + 1)
        {
            sortedNotes.Add(null);
        }
        if (sortedNotes[n.pulse] == null)
        {
            sortedNotes[n.pulse] = new List<Note>();
        }
        sortedNotes[n.pulse].Add(n);
    }

    public void DeleteNote(Note n)
    {
        // Delete from serialized fields.
        SoundChannel channel = soundChannels.Find(
            (SoundChannel c) => { return c.name == n.sound; });
        if (channel == null)
        {
            throw new Exception(
                $"Sound channel {n.sound} not found in pattern when deleting.");
        }
        channel.notes.Remove(n);

        // Delete from unserialized fields.
        if (sortedNotes.Count < n.pulse + 1)
        {
            throw new Exception(
                $"Pulse {n.pulse} not found in pattern when deleting.");
        }
        sortedNotes[n.pulse].Remove(n);
    }

    // Throws no exception if note doesn't exist.
    public void DeleteNoteAt(int pulse, int lane)
    {
        // Find the note first.
        if (sortedNotes.Count < pulse + 1) return;
        if (sortedNotes[pulse] == null) return;
        Note n = sortedNotes[pulse].Find((Note note) =>
        {
            return note.lane == lane;
        });
        if (n == null) return;

        // Delete from serialized fields.
        SoundChannel channel = soundChannels.Find(
            (SoundChannel c) => { return c.name == n.sound; });
        if (channel == null) return;
        channel.notes.Remove(n);

        // Delete from unserialized fields.
        sortedNotes[pulse].Remove(n);
    }
}

[Serializable]
public enum ControlScheme
{
    Touch = 0,
    Keys = 1,
    KM = 2
}

[Serializable]
public class PatternMetadata
{
    public string patternName;
    public int level;
    public ControlScheme controlScheme;

    // The backing track played in game.
    // This always plays from the beginning.
    // If no keysounds, this should be the entire track.
    public string backingTrack;
    // Beat 0 starts at this time.
    public double firstBeatOffset;

    // These can be changed by events.
    public double initBpm;
    // BPS: beats per scan.
    public int bps;
}

[Serializable]
public class BpmEvent
{
    public int pulse;
    public double bpm;
}

[Serializable]
public class SoundChannel
{
    // Sound file name.
    public string name;
    // Notes using this sound.
    public List<Note> notes;
    public List<DragNote> dragNotes;
}

[Serializable]
public enum NoteType
{
    Basic,
    ChainHead,
    Chain,
    HoldStart,
    HoldEnd,
    Drag,
    RepeatHead,
    RepeatHeadHold,
    Repeat,
    RepeatHoldStart,
    RepeatHoldEnd,
}

[Serializable]
public class Note
{
    public int lane;
    public int pulse;
    public NoteType type;

    // Following fields are filled at runtime, and most
    // only apply to specific types.

    [NonSerialized]
    public string sound;
    // ChainHead and Chain only
    [NonSerialized]
    public Note nextChainNode;
    // Chain only
    [NonSerialized]
    public Note prevChainNode;
    // HoldStart, RepeatHeadHold, RepeatHoldStart only
    [NonSerialized]
    public Note holdEnd;
    // HoldEnd, RepeatHoldEnd only
    [NonSerialized]
    public Note holdStart;
    // Repeat* only
    [NonSerialized]
    public Note nextRepeatNote;
    // Repeat* only
    [NonSerialized]
    public Note prevRepeatNote;
}

[Serializable]
public class DragNotePath
{
    public int lane;
    public int pulse;
}

[Serializable]
public class DragNote : Note
{
    public List<DragNotePath> path;
}