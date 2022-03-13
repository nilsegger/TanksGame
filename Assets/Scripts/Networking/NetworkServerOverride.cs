using UnityEngine;

[System.Serializable]
public abstract class NetworkServerOverride<Type>
{
    public float resetPositionAfterMismatchTime;
    public float serverOverridePositionAfterMaxDistance;
    public float interpolationDuration;

    protected NetworkServerOverride(float resetPositionAfterMismatchTime, float serverOverridePositionAfterMaxDistance, float interpolationDuration)
    {
        this.resetPositionAfterMismatchTime = resetPositionAfterMismatchTime;
        this.serverOverridePositionAfterMaxDistance = serverOverridePositionAfterMaxDistance;
        this.interpolationDuration = interpolationDuration;
        this._offsetCounter = 0.0f;
        this._interpolatingTimer = 0.0f;
    }

    protected  float _offsetCounter;
    protected  float _interpolatingTimer;
    protected  Type _interpolatingStart;
    
    private void StartCountingMismatch(float time)
    {
        _offsetCounter += time;
    }

    public bool IsOverrideDistance(float distance)
    {
        return distance >= serverOverridePositionAfterMaxDistance;
    }

    private bool ShouldOverride()
    {
        return _offsetCounter >= this.resetPositionAfterMismatchTime;
    }

    public void Reset()
    {
        _offsetCounter = 0.0f;
    }

    private bool IsInterpolating()
    {
        return _interpolatingTimer != 0.0f && _interpolatingTimer < interpolationDuration;
    }
    
    private Type StartInterpolate(Type position, Type target, float time)
    {
        _interpolatingTimer = 0.0f;
        _interpolatingStart = position;
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
        
        if (IsOverrideDistance(distance))
        {
            StartCountingMismatch(time);
            if (ShouldOverride())
            {
                updated = StartInterpolate(current, server, Time.deltaTime);
                Reset();
                return true;
            }

            // Debug.DrawLine(transform.position, _serverPosition.Value, Color.red);
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
    public NetworkServerOverridePosition(float resetPositionAfterMismatchTime, float serverOverridePositionAfterMaxDistance, float interpolationDuration) : base(resetPositionAfterMismatchTime, serverOverridePositionAfterMaxDistance, interpolationDuration)
    {
        this._interpolatingStart = Vector3.zero;
    }

    protected override Vector3 Interpolate(Vector3 target, float time)
    {
        _interpolatingTimer += time;
        return Vector3.Lerp(_interpolatingStart, target, 1.0f / interpolationDuration * _interpolatingTimer);
    }
}

public class NetworkServerOverrideFloat : NetworkServerOverride<float>
{
    public NetworkServerOverrideFloat(float resetPositionAfterMismatchTime, float serverOverridePositionAfterMaxDistance, float interpolationDuration) : base(resetPositionAfterMismatchTime, serverOverridePositionAfterMaxDistance, interpolationDuration)
    {
        this._interpolatingStart = 0.0f;
    }

    protected override float Interpolate(float target, float time)
    {
        _interpolatingTimer += time;
        return Mathf.Lerp(_interpolatingStart, target, 1.0f / interpolationDuration * _interpolatingTimer);
    }
}