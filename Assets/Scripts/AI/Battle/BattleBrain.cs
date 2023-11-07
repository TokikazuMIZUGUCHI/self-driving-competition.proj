using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 競争用のBrainクラス
/// </summary>
public abstract class BattleBrain : ScriptableObject
{
    /// <summary>
    /// 表示用の名前
    /// </summary>
    [Tooltip("表示用の名前")]
    public string Name = "Player 1";

    /// <summary>
    /// 観測状態の設定．Start()でAgentExecutorに参照されてAgentに反映される
    /// </summary>
    public List<double> ObservationConfig = new List<double>();

    /// <summary>
    /// 初期化処理．Start()でAgentExecutorによって呼び出される．
    /// </summary>
    public virtual void Initialize() { }

    /// <summary>
    /// 観測状態から行動を決定する．FixedUpdate()でAgentExecutorによって呼び出される．
    /// </summary>
    /// <param name="observation">観測状態</param>
    /// <returns>行動</returns>
    public abstract double[] GetAction(List<double> observation);

    /// <summary>
    /// 観測状態のうち必要な要素のみを取り出し並び替える
    /// </summary>
    /// <remarks>
    /// 例：
    /// indexesToUseが{0,5,3}の時，
    /// 返り値は{observation[0], observation[5], observation[3]}
    /// </remarks>
    /// <param name="observation">観測状態</param>
    /// <param name="indexesToUse">取り出す要素のインデックス</param>
    /// <returns>indexesToUseの順に並び替えた観測状態</returns>
    protected List<double> RearrangeObservation(List<double> observation, List<int> indexesToUse)
    {
        if(observation == null || indexesToUse == null) return null;

        List<double> rearranged = new List<double>();
        foreach(int index in indexesToUse)
        {
            if(index >= observation.Count)
            {
                rearranged.Add(0);
                continue;
            }
            rearranged.Add(observation[index]);
        }

        return rearranged;
    }
}
