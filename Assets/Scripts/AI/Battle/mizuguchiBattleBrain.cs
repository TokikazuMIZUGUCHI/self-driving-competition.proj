using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(menuName = "BattleBrain/mizuguchiBattleBrain")]
// public class mitsukeBattleBrain : BattleBrain
// {
//   public override double[] GetAction(List<double> observation) { 
//     return brain.GetAction(ProcessObservation(observation));
//   }
// }

public class mizuguchiBattleBrain : BattleBrain
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
        // 行動配列のサイズを3に拡張します。ステアリング、アクセラレーション、ブレーキングの3つのアクションを含む配列です。
        double[] action = new double[3];

        // センサー情報を解析して行動を決定します。
        // 前方の壁までの平均距離を取得します。
        double distanceToWallAhead = observation.GetRange(0, 5).Average(); // 前方の壁までの平均距離

        // 車両の現在速度を取得します。
        Vector3 currentSpeed = new Vector3((float)observation[40], 0, (float)observation[42]);

        // 前方向ベクトルの情報を使用して、目標方向と現在の方向との差を取得します。
        Vector3 forwardVector = new Vector3((float)observation[43], 0, (float)observation[45]);
        Vector3 desiredDirection = forwardVector.normalized; // 希望する方向

        // ステアリング値を計算します。
        action[0] = CalculateSteering(desiredDirection, currentSpeed);

        // アクセラレーション値を計算します。
        action[1] = CalculateAcceleration(distanceToWallAhead, currentSpeed);

        // ブレーキング値を決定するロジックをここに追加します。今のところブレーキをかけないと仮定します。
        action[2] = 100.0; // ブレーキングなし。

        

        return action; // 計算した行動配列を返します。
    }

    private double CalculateSteering(Vector3 desiredDirection, Vector3 currentSpeed)
    {
        // 車両の現在の向きと目的地点の方向との間の角度を計算します。
        float angleToTarget = Vector3.SignedAngle(currentSpeed, desiredDirection, Vector3.up);

        // ステアリングの角度を、車両の可能なステアリング範囲内で計算します。
        // ここでは、-1は完全に左に、1は完全に右にステアリングを意味します。
        // angleToTargetを使用して、ステアリング値を決定します。
        // 実際の値は、車両のステアリング感度や現在の速度に応じて調整する必要があります。
        double steeringValue = Mathf.Clamp(angleToTarget / maxSteeringAngle, -1.0f, 1.0f);

        return (float)steeringValue;
    }

    [SerializeField] private float maxSteeringAngle = 30.0f; // 最大ステアリング角度（度）
    [SerializeField] private float MaxSpeed = 200.0f; // 車両の最大速度（おそらくkm/hまたはm/s）
    [SerializeField] private float SafeDistance = 50.0f; // 安全距離（メートル）

    private double CalculateAcceleration(double distanceToWallAhead, Vector3 currentSpeed)
    {
        // 現在の速度と前方の壁までの距離に基づいて加速または減速を決定します。
        // ここでは、速度を増やすか、減速するかの簡単な例を示します。
        double targetSpeed = MaxSpeed; // 最大速度
        double currentForwardSpeed = currentSpeed.z; // Z成分は前方向の速度とします。

        // 壁までの距離に応じて目標速度を調整します。
        if (distanceToWallAhead < SafeDistance) // SafeDistanceは安全に停止できる距離
        {
            targetSpeed *= (distanceToWallAhead / SafeDistance); // 壁までの距離が近いほど速度を落とします。
        }

        // 現在の速度と目標速度の差に基づいて加速値を計算します。
        
        double accelerationValue = (targetSpeed - currentForwardSpeed) / targetSpeed; // 目標に対する加速度比率

        // 加速値を-1から1の範囲にクランプします。1は全加速、-1は全ブレーキを意味します。
        float clampedAcceleration = Mathf.Clamp((float)accelerationValue, -1.0f, 1.0f);

        return accelerationValue;
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
        return RearrangeObservation(observation, new List<int>(){0, 2, 4, 5, 7, 9, 10, 12, 14, 20, 21, 22, 23, 24, 40, 42, 43, 44, 45});
    }
}
