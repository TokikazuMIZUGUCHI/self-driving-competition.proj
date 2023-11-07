// SerialID: [77a855b2-f53d-4b80-9c94-c40562952b74]
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class QEnvironment : Environment
{
    [SerializeField] private GameObject gObject = null;
    private GameObject GObject => gObject;

    [SerializeField] private int actionSize = 6;
    private int ActionSize { get { return actionSize; } }

    [SerializeField] private int stateSize = 4;
    private int StateSize { get { return stateSize; } }

    [Header("UI References"), SerializeField] private Text episodeText = null;
    private Text EpisodeText { get { return episodeText; } }

    private QBrain QLBrain { get; set; }
    private Agent LearningAgent { get; set; }

    private int PrevState { get; set; }
    private int PrevAction { get; set; }
    private int EpisodeCount { get; set; }

    void Start() {
        QLBrain = new QBrain(StateSize, ActionSize);
        LearningAgent = GObject.GetComponent<Agent>();
        PrevState = -1;
        UpdateText();
    }

    private void FixedUpdate() {
        AgentUpdate(LearningAgent, QLBrain);
        if(LearningAgent.IsDone) {
#if UNITY_EDITOR
            var path = string.Format("Assets/LearningData/Q/{0}.bytes", EditorSceneManager.GetActiveScene().name);
            QLBrain.Save(path);
#endif
            EpisodeCount++;

            LearningAgent.Reset();
            PrevState = -1;
            UpdateText();
        }
    }

    private void AgentUpdate(Agent a, QBrain b) {
        // 1ステップ前の行動によって得た結果の取得．
        int currentState = a.GetState();
        float reward = a.Reward;
        bool isDone = a.IsDone;

        // 1ステップ前の行動とその結果を用いてQテーブルを更新する．
        // リセット直後は直前の行動が使えないため更新しない．
        if(PrevState >= 0)
        {
            b.UpdateTable(PrevState, currentState, PrevAction, reward, isDone);
        }

        // 現在の状態からとる行動を決定する．
        int actionNo = b.GetAction(currentState);
        double[] action = a.ActionNumberToVectorAction(actionNo);
        a.AgentAction(action);

        // 状態と行動を記録する．
        PrevState = currentState;
        PrevAction = actionNo;
    }

    private void UpdateText() {
        EpisodeText.text = "Episode: " + EpisodeCount;
    }
}
