// SerialID: [e4a22a75-a938-4302-8b9a-d405c01db428]
using System.Collections.Generic;
using UnityEngine;

public abstract class Agent : MonoBehaviour
{
    public bool IsDone { get; private set; }
    public bool IsFrozen { get; private set; }

    public float Reward { get; private set; }

    public void SetReward(float reward) {
        Reward = reward;
    }

    public void AddReward(float reward) {
        Reward += reward;
    }

    public abstract int GetState();

    public abstract void AgentAction(double[] vectorAction);
    public abstract float GetDistance();

    public abstract void AgentReset();

    public abstract void Stop();

    public abstract double[] ActionNumberToVectorAction(int ActionNumber);

    public void Done()
    {
        IsDone = true;
    }

    public void Reset()
    {
        Stop();
        AgentReset();
        IsDone = false;
        IsFrozen = false;
        SetReward(0);
    }

    public abstract List<double> GetAllObservations();
    public abstract List<double> CollectObservations();
    public abstract List<double> OriginalObservations();

    public virtual void SetAgentConfig(List<double> config) { }

    // parameter for battle
    public bool isBattle;
    public AgentExecutor agentExecutor;
    public int agentIndex;

    public abstract void GoStraight();

}
