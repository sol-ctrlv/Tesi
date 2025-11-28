
using UnityEngine;

[System.Serializable]
public class Timer
{
    [SerializeField] private float counter, timer;
    [SerializeField] private bool shouldTick, shouldLoop;

    public Timer(float inTimer, bool shouldLoop = false)
    {
        timer = inTimer;
        counter = inTimer;
        shouldTick = true;
        this.shouldLoop = shouldLoop;
    }

    public bool Tick(float deltaTime)
    {
        if (!shouldTick)
            return false;

        counter -= deltaTime;

        if (counter < 0)
        {
            if (shouldLoop)
            {
                Reset();
            }
            else
            {
                shouldTick = false;
            }

            return true;
        }

        return false;
    }

    public void Reset()
    {
        counter = timer;
        shouldTick = true;
    }

    public void RandomResetTimer()
    {
        shouldTick = true;
    }

    public void Set(float newTimer)
    {
        timer = newTimer;
    }
}
