using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class PyNEEnvironment : Environment
{
    [Header("Settings"), SerializeField] private int totalPopulation = 100;
    private int TotalPopulation { get { return totalPopulation; } }

    [SerializeField] private int tournamentSelection = 85;
    private int TournamentSelection { get { return tournamentSelection; } }

    [SerializeField] private int eliteSelection = 4;
    private int EliteSelection { get { return eliteSelection; } }

    [SerializeField] private int inputSize = 7;
    private int InputSize { get { return inputSize; } }

    [SerializeField] private int hiddenSize = 8;
    private int HiddenSize { get { return hiddenSize; } }

    [SerializeField] private int hiddenLayers = 1;
    private int HiddenLayers { get { return hiddenLayers; } }

    [SerializeField] private int outputSize = 4;
    private int OutputSize { get { return outputSize; } }

    [SerializeField] private int nAgents = 8;
    private int NAgents { get { return nAgents; } }

    [Header("Agent Prefab"), SerializeField] private GameObject gObject = null;
    private GameObject GObject => gObject;

    [Header("UI References"), SerializeField] private Text populationText = null;
    private Text PopulationText { get { return populationText; } }

    private float GenBestRecord { get; set; }

    private float SumReward { get; set; }
    private float AvgReward { get; set; }

    private List<GameObject> GObjects { get; } = new List<GameObject>();
    private List<Agent> Agents { get; } = new List<Agent>();
    private int Generation { get; set; }

    private float BestRecord { get; set; }

    private List<AgentPair> AgentsSet { get; } = new List<AgentPair>();
    
    private int CurrentBrainID {get; set; }

    [Header("UDP Settings"), SerializeField]
    private string ip = "127.0.0.1";
    private string IP => ip;

    [SerializeField] private int infoSrcPort = 50008;
    private int InfoSrcPort => infoSrcPort;
    [SerializeField] private int actionSrcPort = 50009;
    private int ActionSrcPort => actionSrcPort;
    [SerializeField] private int generationSrcPort = 50010;
    private int GenerationSrcPort => generationSrcPort;
    [SerializeField] private int lostSrcPort = 50011;
    private int LostSrcPort => lostSrcPort;

    [SerializeField] private int dstPort = 50007;
    private int DstPort => dstPort;


    private UDP InfoClient { get; } = new UDP();
    private UDP ActionClient { get; } = new UDP();
    private UDP GenerationClient { get; } = new UDP();
    private UDP LostClient { get; } = new UDP();


    float[] reward_storages;
    string current_received_str = "";
    string lostRewardIDs = "";
    bool waitFlag = true;
    int counter = 0;

    void Start() {
        InfoClient.Set(IP, InfoSrcPort, DstPort);
        ActionClient.Set(IP, ActionSrcPort, DstPort);
        GenerationClient.Set(IP, GenerationSrcPort, DstPort);
        LostClient.Set(IP, LostSrcPort, DstPort);
        string EnvInfo = "GenerateFirstPop," + TotalPopulation.ToString() + "," 
                                        + InputSize.ToString() + ","
                                        + HiddenSize.ToString() + ","
                                        + HiddenLayers.ToString() + ","
                                        + OutputSize.ToString();
        InfoClient.Send(EnvInfo, _=>{});
        reward_storages = new float[TotalPopulation];

        for(int i = 0; i < NAgents; i++) {
            var obj = Instantiate(GObject);
            obj.SetActive(true);
            GObjects.Add(obj);
            Agents.Add(obj.GetComponent<Agent>());
        }
        BestRecord = -9999;
        SetStartAgents();
    }

    void SetStartAgents() {
        CurrentBrainID = 0;
        AgentsSet.Clear();
        var size = Math.Min(NAgents, TotalPopulation);
        for(var i = 0; i < size; i++) {
            AgentsSet.Add(new AgentPair {
                agent = Agents[i],
                brainID = CurrentBrainID
            });
            CurrentBrainID += 1;
        }
    }

    void FixedUpdate() {
        AgentsUpdate(AgentsSet);
        counter += 1;
        AgentsSet.RemoveAll(p => {
            if(p.agent.IsDone) {
                p.agent.Stop();
                p.agent.gameObject.SetActive(false);
                float r = p.agent.Reward;
                BestRecord = Mathf.Max(r, BestRecord);
                GenBestRecord = Mathf.Max(r, GenBestRecord);
                InfoClient.Send("SetReward,"+p.brainID.ToString()+","+r.ToString(), _=>{});
                SumReward += r;
                reward_storages[p.brainID] = r;
            }
            return p.agent.IsDone;
        });

        if(CurrentBrainID >= TotalPopulation && AgentsSet.Count == 0) {
            GenerationClient.SendAndWait("CheckFinishGeneration", _=>{ if (_ == "GoNextGen") { this.waitFlag = false; }});
            if (!waitFlag) {
                this.lostRewardIDs = "";
                SetNextGeneration();
                waitFlag = true;
            } else {
                try
                {
                    LostClient.SendAndWait("CheckLost", lostIDs => { if (lostIDs != "") { this.lostRewardIDs = lostIDs; }});
                }
                catch(TimeoutException)
                {
                    // タイムアウトは無視
                } // それ以外の例外はそのまま発生させる
                if (lostRewardIDs == "") {
                    return;
                }
                string[] lostRewardID_strs = lostRewardIDs.Split(',');
                for (int i = 0; i < lostRewardID_strs.Length; i++) {
                    if (lostRewardID_strs[i] == "") {
                        continue;
                    }
                    int lostRewardID = Int32.Parse(lostRewardID_strs[i]);
                    if (lostRewardID < 0 || TotalPopulation <= lostRewardID) {
                        continue;
                    }
                    InfoClient.Send("SetReward,"+lostRewardID+","+reward_storages[lostRewardID], _=>{});
                }
            }
        }
        else {
            SetNextAgents();
        }
    }

    private void AgentsUpdate(List<AgentPair> AgentsSet) {
        string ActionInfo = "GetAction";
        AgentsSet.ForEach(
                p => { 
                    if (!p.agent.IsDone) {
                        var observation = p.agent.CollectObservations();
                        ActionInfo += "," + p.brainID.ToString();
                        for (int i = 0; i < observation.Count; i++) {
                            ActionInfo += "," + observation[i].ToString();
                        }
                    }
                });
        string now_current_str = current_received_str;
        try
        {
            //ここ以外で、ActionClientに命令を送ると混線する可能性あり。
            ActionClient.SendAndWait(ActionInfo, 
                received_str => {
                    if (received_str != this.current_received_str) {
                        this.current_received_str = received_str;
                    }
                });
        }
        catch(Exception ex)
        {
            Debug.LogException(new Exception("Error in \"GetAction\"", ex));
        }
        

        string[] action_strs = current_received_str.Split(' ');
        double[][] actions = new double[TotalPopulation][];
        for (int i = 0; i < action_strs.Length / (OutputSize + 1) ; i++) {
            int actionNum = i * (OutputSize + 1) + 1;
            int agentNum = 0;
            Int32.TryParse(action_strs[actionNum], out agentNum);
            double [] action = new double[OutputSize];
            for (int j = 0; j < OutputSize; j++) {
                action[j] = double.Parse(action_strs[actionNum + 1 + j]);
            }
            actions[agentNum] = action;
        }
        
        AgentsSet.ForEach(
                p => { 
                    if (!p.agent.IsDone) {
                        if (actions[p.brainID] != null) {
                            p.agent.AgentAction(actions[p.brainID]);
                        }
                    }
                });
    }

    private void SetNextAgents() {
        int size = Math.Min(NAgents - AgentsSet.Count, TotalPopulation - CurrentBrainID);
        for(var i = 0; i < size; i++) {
            var nextAgent = Agents.First(a => a.IsDone);
            nextAgent.Reset();
            AgentsSet.Add(new AgentPair {
                agent = nextAgent,
                brainID = CurrentBrainID++
            });
        }
        UpdateText();
    }

    private void SetNextGeneration() {
        AvgReward = SumReward / TotalPopulation;
        GenPopulation();
        SumReward = 0;
        GenBestRecord = -9999;
        Agents.ForEach(a => a.Reset());
        SetStartAgents();
        UpdateText();
    }

    private void GenPopulation() {
        string EnvInfo = "GenerateNextPop,"+Generation.ToString()+","+EliteSelection.ToString()+","+TournamentSelection.ToString();
        GenerationClient.Send(EnvInfo);
        if (waitFlag) {
            return;
        }
        Generation++;
    }

    private void UpdateText() {
        PopulationText.text = "Population: " + (CurrentBrainID) + "/" + TotalPopulation
            + "\nGeneration: " + (Generation + 1)
            + "\nBest Record: " + BestRecord
            + "\nBest this gen: " + GenBestRecord
            + "\nAverage: " + AvgReward;
    }

    private struct AgentPair
    {
        public int brainID;
        public Agent agent;
    }

    private void OnDestroy() {
        InfoClient.Dispose();
        ActionClient.Dispose();
        GenerationClient.Dispose();
        LostClient.Dispose();
    }
}
