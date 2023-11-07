using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BattleBrain/QBattleBrain")]
public class QBattleBrain : BattleBrain
{
    [SerializeField] private TextAsset brainData = null;

    private QBrain brain;

    public override void Initialize() {
        base.Initialize();
        try
        {
            brain = QBrain.Load(brainData);
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to load QBrain.", ex);
        }
    }

    public override double[] GetAction(List<double> observation)
    {
        int state = GetState(observation);
        int actionNumber = brain.GetActionWithoutEpsilon(state);
        double[] action = ActionNumberToVectorAction(actionNumber);
        return action;
    }

    /// <summary>
    /// 連続な観測状態を離散化する．
    /// </summary>
    /// <remarks>
    /// 既存のQEnvironmentの学習環境と同じ離散化方法．
    /// </remarks>
    /// <param name="observation">観測状態</param>
    /// <returns>離散状態</returns>
    protected int GetState(List<double> observation) {
        const int stateDivide = 3;
        int state = 0;

        int sensorStartIndex = 0;
        int sensorCount = 5;
        for(int i = sensorStartIndex; i < sensorStartIndex + sensorCount; i++) {
            float sensorValue = (float)observation[i];
            int discretized = Mathf.FloorToInt(Mathf.Lerp(0, stateDivide - 1, sensorValue));
            if(observation[i] >= 0.99f) {
                discretized = stateDivide - 1;
            }
            state += (int)(discretized * Mathf.Pow(stateDivide, i));
        }

        int speedState;
        int velocityStartIndex = 40;
        float speed = Vector3.Magnitude(new Vector3(
            (float)observation[velocityStartIndex], 
            (float)observation[velocityStartIndex + 1], 
            (float)observation[velocityStartIndex + 2]));
        // 速度は1/5にスケールされているのでスケールをもとに戻す．
        speed *= 5;
        if      (speed < 10) speedState = 0;
        else if (speed < 15) speedState = 1;
        else                 speedState = 2;
        state += speedState * (int)Mathf.Pow(stateDivide, sensorCount);

        return state;
    }

    /// <summary>
    /// 離散行動を連続値に変換する．
    /// </summary>
    /// <param name="actionNumber">離散行動</param>
    /// <returns>連続行動</returns>
    protected double[] ActionNumberToVectorAction(int actionNumber) {
        var action = new double[3];
        var steering = 0.0d;
        var braking = 0.0d;
        if(actionNumber % 6 == 1) {
            steering = 1d;
        }
        else if(actionNumber % 6 == 2) {
            steering = -1d;
        }
        else if(actionNumber % 6 == 3) {
            steering = 0.5d;
        }
        else if(actionNumber % 6 == 4) {
            steering = -0.5d;
        }
        else if(actionNumber % 6 == 5) {
            braking = 0.5d;
        }

        var gasInput = 0.5d;
        action[0] = steering;
        action[1] = gasInput;
        action[2] = braking;
        return action;
    }
}
