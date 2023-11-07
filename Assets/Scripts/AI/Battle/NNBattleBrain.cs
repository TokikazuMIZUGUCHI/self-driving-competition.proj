using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BattleBrain/NNBattleBrain")]
public class NNBattleBrain : BattleBrain
{
    [SerializeField] private TextAsset brainData = null;

    [Header("Observation")]
    [Tooltip("取得可能な全ての観測状態を使用する")]
    [SerializeField] private bool usesAllObservations = false;

    private NNBrain brain;

    public override void Initialize() {
        base.Initialize();
        try
        {
            brain = NNBrain.Load(brainData);
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to load NNBrain.", ex);
        }
    }

    public override double[] GetAction(List<double> observation)
    {
        return brain.GetAction(ProcessObservation(observation));
    }

    protected virtual List<double> ProcessObservation(List<double> observation)
    {
        if(usesAllObservations) return observation;
        return GetLegacyObservation(observation);
    }

    /// <summary>
    /// 既存のNEEnvironmentの学習環境と同じになるように観測状態を加工する．
    /// </summary>
    /// <remarks>
    /// 返り値のリストの内容は，
    /// 
    /// | インデックス | 内容 |
    /// | --- | --- |
    /// | 0--4 | 前方5方向の対壁センサー |
    /// | 5, 6 | 自車のローカル速度のx, z成分 |
    /// 
    /// </remarks>
    /// <param name="observation">観測状態</param>
    /// <returns>加工した観測状態</returns>
    protected List<double> GetLegacyObservation(List<double> observation) {
        return RearrangeObservation(observation, new List<int>(){0, 1, 2, 3, 4, 40, 42});
    }
}
