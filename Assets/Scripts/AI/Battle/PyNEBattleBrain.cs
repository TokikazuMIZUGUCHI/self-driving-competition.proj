using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BattleBrain/PyNEBattleBrain")]
public class PyNEBattleBrain : BattleBrain
{
    [Header("Observation")]
    [Tooltip("取得可能な全ての観測状態を使用する")]
    [SerializeField] private bool usesAllObservations = false;

    [SerializeField] private int inputSize = 7;
    private int InputSize { get { return inputSize; } }
    [SerializeField] private int hiddenSize = 8;
    private int HiddenSize { get { return hiddenSize; } }
    [SerializeField] private int hiddenLayers = 1;
    private int HiddenLayers { get { return hiddenLayers; } }
    [SerializeField] private int outputSize = 4;
    private int OutputSize { get { return outputSize; } }

    [Header("UDP Settings"), SerializeField]
    private string ip = "127.0.0.1";
    private string IP => ip;

    [SerializeField] private int srcPort = 50009;
    private int SrcPort => srcPort;

    [SerializeField] private int dstPort = 50007;
    private int DstPort => dstPort;

    private UDP Client { get; } = new UDP();

    string current_received_str = "";

    public override void Initialize() {
        base.Initialize();
        try
        {
            Client.Set(IP, SrcPort, DstPort);
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to setup UDP client.", ex);
        }

        try
        {
            string NNInfo = "LoadNN,"  + InputSize.ToString() + ","
                                        + HiddenSize.ToString() + ","
                                        + HiddenLayers.ToString() + ","
                                        + OutputSize.ToString();
            Client.SendAndWait(NNInfo,_=>{});       
        }
        catch(Exception ex) {
            throw new Exception("Failed to send Load NN message",ex);
        }

    }

    /// <summary>
    /// UDPを用いてobsevationからactionを取得する．
    /// </summary>
    /// <remarks>
    /// python側に受け渡す情報は下のデータを文字列化し、頭に命令内容をつけたもの
    /// 
    /// | インデックス | 内容 |
    /// | --- | --- |
    /// | 0--4 | 前方5方向の対壁センサー |
    /// | 5, 6 | 自車のローカル速度のx, z成分 |
    /// 
    /// </remarks>
    public override double[] GetAction(List<double> observation)
    {
        List<double> processedObservation=ProcessObservation(observation);
        string ActionInfo="GetActionForBattle";
        for (int i = 0; i < processedObservation.Count; i++) 
        {
            ActionInfo += "," + processedObservation[i].ToString();
        }

        Client.SendAndWait(ActionInfo, 
            received_str => {
                if (received_str != current_received_str) {
                    current_received_str = received_str;
                }
            });
        
        string[] action_strs = current_received_str.Split(' ');
        double[] actions =new double[3];
        for (int j = 0; j < 3; j++) {
                actions[j] = double.Parse(action_strs[j+1]);
            }
        return actions;
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

    protected void OnDestroy() {
        Client?.Dispose();
    }
}
