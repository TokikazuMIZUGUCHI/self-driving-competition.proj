// SerialID: [e4a22a75-a938-4302-8b9a-d405c01db428]
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class AgentExecutor : MonoBehaviour
{
    [Serializable]
    public class AgentItem
    {
        public Agent agent;
        public BattleBrain brain;
    }
    [SerializeField] private List<AgentItem> agents = null;
    private List<AgentItem> Agents => agents;
    private List<float> scores=new List<float>(){0,0};

    [Header("UI References"), SerializeField] private Text populationText = null;
    [Header("name1 UI"), SerializeField] private Text name1Text = null;
    [Header("name2 UI"), SerializeField] private Text name2Text = null;
    [Header("win1 UI"), SerializeField] private Text win1Text = null;
    [Header("win2 UI"), SerializeField] private Text win2Text = null;
    [Header("gameover1 UI"), SerializeField] private Text gameover1Text = null;
    [Header("gameover2 UI"), SerializeField] private Text gameover2Text = null;
    [Header("time1 UI"), SerializeField] private Text time1Text = null;
    [Header("time2 UI"), SerializeField] private Text time2Text = null;

    [Header("Debug")]
    [SerializeField] private bool suppressLog = false;

    private Text PopulationText { get { return populationText; } }
    private List<float> lastScores = new List<float>();
    private bool alreadyGoal=false;
    private bool time1fix=false;
    private bool time2fix=false;


    private void Start() {
        if(suppressLog) Debug.unityLogger.filterLogType = LogType.Warning;
        if(Agents == null || Agents.Count == 0) {
            this.enabled = false;
            return;
        }
        name1Text.text = TryGetBrainName(0);
        name2Text.text = TryGetBrainName(1);
        name1Text.enabled=true;
        name2Text.enabled=true;
        time1Text.enabled=true;
        time2Text.enabled=true;

        InitializeAgent(0);
        InitializeAgent(1);
    }

    private void FixedUpdate() {
        for(int i = 0; i < 2; i++) {
            try
            {
                AgentUpdate(i);
            }
            catch (TimeoutException)
            {
                Debug.LogError($"Timeout during AgentUpdate of Agent {i}.");
            }
            catch (Exception ex)
            {
                Debug.LogException(new Exception($"Exception in AgentUpdate of Agent {i}.", ex));
            }
        }
        UpdateText();
    }

    private string TryGetBrainName(int index) {
        if(Agents == null) return null;
        if(Agents[index] == null) return null;
        if(Agents[index].brain == null) return null;
        return Agents[index].brain.Name;
    }

    /// <summary>
    /// 指定したインデックスのAgentItemの初期化
    /// </summary>
    /// <param name="index">AgentItemのインデックス</param>
    private void InitializeAgent(int index) {
        if(Agents == null || index < 0 || Agents.Count <= index) return;

        try
        {
            Agents[index].agent.isBattle=true;
            Agents[index].agent.agentExecutor=this;
            Agents[index].agent.agentIndex=index;

            Agents[index].brain.Initialize();
            Agents[index].agent.SetAgentConfig(Agents[index].brain.ObservationConfig);
        }
        catch (Exception ex)
        {
            Debug.LogException(new Exception($"Failed to initialize Agent {index}.", ex));
            this.enabled = false;
            return;
        }
    }

    /// <summary>
    /// 状態の観測 -> 行動の決定 -> 行動の実行 の1ステップ
    /// </summary>
    /// <param name="index">AgentItemのインデックス</param>
    private void AgentUpdate(int index) {
        if(Agents == null || index < 0 || Agents.Count <= index) return;

        if(Agents[index].agent.IsDone) {
            Agents[index].agent.Stop();
            GameOver(index);
            return;
        }

        List<double> observation = Agents[index].agent.GetAllObservations();
        double[] action = Agents[index].brain.GetAction(observation);
        Agents[index].agent.AgentAction(action);

        scores[index]=Agents[index].agent.GetDistance();
    }

    private void UpdateText() {
        string text="";
        for (int i = 0; i < scores.Count; i++)
        {
            text+=TryGetBrainName(i)+" Car"+(i+1).ToString()+" Score: "+scores[i].ToString()+"\n";
        }
        PopulationText.text = text;
        if(time1fix==false){
            time1Text.text="time:"+Time.time.ToString();
        }
        if(time2fix==false){
            time2Text.text="time:"+Time.time.ToString();
        }
    }

    public void Win(int agentIndex){
        if(alreadyGoal==false){
            if(agentIndex==0){
                win1Text.enabled=true;
            }else{
                win2Text.enabled=true;
            }
        }
        if(agentIndex==0){
            time1fix=true;
        }else{
            time2fix=true;
        }
        alreadyGoal=true;
    }

    public void GameOver(int agentIndex){
        if(alreadyGoal==false){
            if(agentIndex==0){
                gameover1Text.enabled=true;
            }else{
                gameover2Text.enabled=true;
            }
        }
        if(agentIndex==0){
            time1fix=true;
        }else{
            time2fix=true;
        }
    }
}
