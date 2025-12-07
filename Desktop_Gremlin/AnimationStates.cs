using System;
using System.Collections.Generic;
using System.Linq;

public class AnimationStates
{
    private readonly Random _random = new Random();
    public bool IsLocked { get; private set; } = false;

    private readonly Dictionary<string, bool> _animationStates = new Dictionary<string, bool>()
    {
        { "Intro", true },
        { "Random", false },
        { "Hover", false },
        { "Idle", false },
        { "Outro", false },
        { "Walking", false },
        { "Grab", false },
        { "WalkIdle", false },
        { "Click", false },
        { "Sleeping", false },
        { "Firing_Left", false },
        { "Firing_Right", false },
        { "Reload", false },
        { "Pat", false },
        { "RandomMovement", false },
        { "Emote1", false },
        { "Emote2", false },
        { "Emote3", false },
        { "Emote4", false },
        { "Dance", false },
        { "FollowItem", false },
    };

    public void ResetAllExceptIdle()
    {
        foreach (var key in _animationStates.Keys.ToList())
        {
            _animationStates[key] = key.Equals("Idle", StringComparison.OrdinalIgnoreCase);
        }
        ChangeIdle();
    }

    public void ChangeIdle()
    {
        int idleState = _random.Next(0, 3);

        switch (idleState)
        {
            case 0:
                Settings.CurrendIdle = 0;
                break;
            case 1:
                Settings.CurrendIdle = 1;
                break;
            case 2:
                Settings.CurrendIdle = 0;
                break;
        }
    }

    public void PlayOutro()
    {
        foreach (var key in _animationStates.Keys.ToList())
        {
            _animationStates[key] = false;
        }
        _animationStates["Outro"] = true;
    }

    public void SetState(string stateName)
    {
        if (IsLocked)
            return;

        string normalized = stateName.Trim();

        if (!_animationStates.ContainsKey(normalized))
            return;

        foreach (var key in _animationStates.Keys.ToList())
            _animationStates[key] = false;

        _animationStates[normalized] = true;
    }

    public bool GetState(string stateName)
    {
        return _animationStates.TryGetValue(stateName, out bool value) && value;
    }

    public bool IsCompletelyIdle()
    {
        foreach (var kv in _animationStates)
        {
            if (!kv.Key.Equals("Idle", StringComparison.OrdinalIgnoreCase) && kv.Value)
                return false;
        }
        return true;
    }

    public void LockState() => IsLocked = true;
    public void UnlockState() => IsLocked = false;
    
    /// <summary>
    /// Get the current active state name for debugging
    /// </summary>
    public string GetCurrentState()
    {
        foreach (var kv in _animationStates)
        {
            if (kv.Value) return kv.Key;
        }
        return "None";
    }
    
    /// <summary>
    /// Get the current animation being played (for debug overlay)
    /// </summary>
    public string GetActiveAnimation()
    {
        var active = _animationStates.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
        return active.Count > 0 ? string.Join(", ", active) : "Idle";
    }
    
    public void SetIntro(bool value)
    {
        if (value)
        {
            SetState("Intro");
        }
    }
    
    /// <summary>
    /// Resets all animation states to default for character switching
    /// </summary>
    public void Reset()
    {
        foreach (var key in _animationStates.Keys.ToList())
        {
            _animationStates[key] = false;
        }
        
        // Set intro to true for new character
        _animationStates["Intro"] = true;
        
        // Reset idle state
        Settings.CurrendIdle = 0;
        IsLocked = false;
    }
}

