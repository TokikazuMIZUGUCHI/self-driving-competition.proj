using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BattleBrain/XBattleBrain")]
public class XBattleBrain : BattleBrain
{
    public override double[] GetAction(List<double> observation) {
        var action = new double[3];
        
        /*
            CarAgent.cs内のOriginalObservation関数内で取得した環境情報をもとにAgentの行動を決定
            SteetInput(ハンドル)
            GasInput(アクセル)
            BrakeInput(ブレーキ)
            の3種類をAIによって決定し、値を返す

            <オリジナルな処理>
        */
        ///  if文で書いた簡単なロジック。自由に変更してください。  ///
        action[0]=0.0f;
        action[1]=0.5f;
        action[2]=0.0f;

        if(observation[2]<1){
            action[0]=1.0f;
            action[1]=0.2f;
            action[2]=0.5f;
            //Debug.Log("angle90");
        }else if(observation[1]<1){
            action[0]=1.0f;
            action[1]=0.2f;
            action[2]=0.5f;
            //Debug.Log("angle70");
        }else if(observation[0]<1){
            action[0]=1.0f;
            //Debug.Log("angle50");
        }else if(observation[3]<0.3){
            action[0]=0.1f;
            action[1]=0.0f;
            action[2]=0.5f;
            //Debug.Log("angle110");
        }else{
            //Debug.Log("nothing hit");
        }
        //Debug.Log(observation[3]);
        if(observation[3]>0.35f){
            //action[1]=0.0f;
            //action[2]=0.5f;
        }
        return action;
    }
}
