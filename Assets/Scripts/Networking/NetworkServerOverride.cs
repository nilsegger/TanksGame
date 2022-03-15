using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityEngine.Assertions;

public struct NetworkServerOverrideSettings
{
    public float ResetPositionAfterMismatchTime;
    public float MaxAllowedDelta;
    public float InterpolationDuration;
};

[System.Serializable]
public abstract class NetworkServerOverride<Type>
{

    private Dictionary<string, NetworkServerOverrideSettings> _settings = new Dictionary<string, NetworkServerOverrideSettings>();
    private string _activeSetting;
    private string _desiredSetting;

    protected NetworkServerOverride()
    {
        OffsetCounter = 0.0f;
        InterpolatingTimer = 0.0f;
    }

    public void AddSetting(string name, NetworkServerOverrideSettings setting)
    {
        Assert.IsFalse(_settings.ContainsKey(name), "Setting already exists with name " + name);
        _settings[name] = setting;
    }

    public void Activate(string settingName, bool force = false)
    {
        if (force || _activeSetting == null)
        {
            _activeSetting = settingName;
            _desiredSetting = null;
        }
        if (IsInterpolating() && !_activeSetting.Equals(settingName)) _desiredSetting = settingName;
        else _activeSetting = settingName;
    }
    
    protected float OffsetCounter;
    protected float InterpolatingTimer;
    protected Type InterpolatingStart;
    
    private void StartCountingMismatch(float time)
    {
        OffsetCounter += time;
    }

    public bool IsOverrideDistance(float distance)
    {
        return distance >= _settings[_activeSetting].MaxAllowedDelta;
    }

    private bool ShouldOverride()
    {
        return OffsetCounter >= _settings[_activeSetting].ResetPositionAfterMismatchTime;
    }

    private void Reset()
    {
        OffsetCounter = 0.0f;
    }

    private bool IsInterpolating()
    {
        return InterpolatingTimer != 0.0f && InterpolatingTimer < _settings[_activeSetting].InterpolationDuration;
    }

    protected float InterpolationDuration()
    {
        return _settings[_activeSetting].InterpolationDuration;
    }
    
    private Type StartInterpolate(Type position, Type target, float time)
    {
        InterpolatingTimer = 0.0f;
        InterpolatingStart = position;
        return Interpolate(target, time);
    }

    protected abstract Type Interpolate(Type target, float time);
    
    public bool CheckForRequiredServerOverride(Type current, Type server, out Type updated, float distance, float time)
    {
        if (IsInterpolating())
        {
            updated = Interpolate(server, time);
            return true;
        }
        
        if (_desiredSetting != null && !_activeSetting.Equals(_desiredSetting))
        {
            _activeSetting = _desiredSetting;
            _desiredSetting = null;
        }
        
        if (IsOverrideDistance(distance))
        {
            StartCountingMismatch(time);
            if (ShouldOverride())
            {
                updated = StartInterpolate(current, server, Time.deltaTime);
                Reset();
                return true;
            }
        }
        else
        {
            Reset();
        }

        updated = default(Type);
        return false;
    }
}

public class NetworkServerOverridePosition : NetworkServerOverride<Vector3>
{
    public NetworkServerOverridePosition()
    {
        InterpolatingStart = Vector3.zero;
    }

    protected override Vector3 Interpolate(Vector3 target, float time)
    {
        InterpolatingTimer += time;
        return Vector3.Lerp(InterpolatingStart, target, 1.0f / InterpolationDuration() * InterpolatingTimer);
    }
}

public class NetworkServerOverrideFloat : NetworkServerOverride<float>
{
    public NetworkServerOverrideFloat()
    {
        InterpolatingStart = 0.0f;
    }

    protected override float Interpolate(float target, float time)
    {
        InterpolatingTimer += time;
        return Mathf.Lerp(InterpolatingStart, target, 1.0f / InterpolationDuration() * InterpolatingTimer);
    }
}

public class NetworkServerOverrideDegrees : NetworkServerOverride<float>
{
    public NetworkServerOverrideDegrees()
    {
        InterpolatingStart = 0.0f;
    }

    protected override float Interpolate(float target, float time)
    {
        // Cases: 340 -> 20 (380)
        // 20 -> 340 (-20)
        Debug.Log(InterpolatingStart + "->" + target);
        InterpolatingTimer += time;
        var d = Mathf.Abs(InterpolatingStart - target);

        if (Mathf.Abs(InterpolatingStart - (target + 360.0f)) < d)
        {
            return Mathf.Lerp(InterpolatingStart, target + 360.0f, 1.0f / InterpolationDuration() * InterpolatingTimer);
        } else if (Mathf.Abs(InterpolatingStart - (target - 360.0f)) < d)
        {
            return Mathf.Lerp(InterpolatingStart, target - 360.0f, 1.0f / InterpolationDuration() * InterpolatingTimer);
        }
        
        return Mathf.Lerp(InterpolatingStart, target, 1.0f / InterpolationDuration() * InterpolatingTimer);
    }
}