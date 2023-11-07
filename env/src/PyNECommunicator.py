from ast import arg
import asyncio
import asyncio_dgram
from parallel_udp.parallel_udp_async import ParallelUDPServerAsync

from PyNNBrain import PyNNBrain
import random
import torch
import copy
import numpy as np
import time
import argparse

class PyNECommunicator() :
    def __init__(self):
        #学習するNNの集団
        self.Brains = []
        self.TotalPopulation = 0
        #学習するNNのパラメータ
        self.InputSize = 0
        self.HiddenSize = 0
        self.HiddenLayers = 0 
        self.OutputSize = 0
        #各NNが評価前か評価後かを判別するためのフラグ
        self.waitFlag = []
        #Battle用のBrain
        self.BattleBrain=None

    #初期集団を作り出す
    def GenerateFirstPop(self):
        self.Brains.clear()
        self.waitFlag.clear()
        for i in range(self.TotalPopulation):
            self.Brains.append(PyNNBrain(self.InputSize, self.HiddenSize, self.HiddenLayers, self.OutputSize))
            self.waitFlag.append(1)
        print("Generate First Population")
    
    #世代交代
    def GenerateNextPop(self,current_generation, elite_selection, tournament_selection) :
        #成績順に並べ替え
        self.Brains.sort(reverse=True)
        #モデルをセーブ
        torch.save(self.Brains[0].state_dict(),'model_weight.pth')
        #エリートを次世代にコピー
        NextBrains = copy.deepcopy(self.Brains[:elite_selection])
        #トーナメントによる淘汰と突然変異による次世代の生成
        while (len(NextBrains) < len(self.Brains)):
            TournamentMembers = random.sample(self.Brains, tournament_selection)
            TournamentMembers.sort(reverse=True)
            NextBrain = copy.deepcopy(TournamentMembers[0])
            NextBrain.Mutate(current_generation)
            NextBrains.append(NextBrain)
            NextBrain = copy.deepcopy(TournamentMembers[1])
            NextBrain.Mutate(current_generation)
            NextBrains.append(NextBrain)

        #世代交代とそれに伴う初期化
        for i in range(len(self.Brains)):
            self.Brains[i].load_state_dict(NextBrains[i].state_dict())
            self.Brains[i].Reward = -9999
            self.waitFlag[i] = 1 
        print("Generate Next Population")

    #成績を表示
    def ShowBrainScoreList(self):
        for id,b in enumerate(self.Brains):
            print(id, b.Reward)

    #成績を受け取れていない個体のIDを文字列に詰めて返す
    def CheckLostID(self):
        LostIDs = ""
        for id, waiting in enumerate(self.waitFlag):
            if (waiting) :
                LostIDs += str(id)
                if (id != len(self.waitFlag) - 1) :
                    LostIDs += ","
        return LostIDs

    #Csスクリプトからの指示
    def receive(self, message):
        #C#側のClientに送る文字列の初期化
        action_txt = generation_txt = ""
        lost_text = None

        #受け取った文字列の前処理
        received_obj = message.split(",")

        #C#からの指示内容の取得
        unityOrder = received_obj[0]

        #最初の世代を作る
        if (unityOrder == "GenerateFirstPop"):
            #集団の大きさ,NNのパラメータを取得
            self.TotalPopulation = int(received_obj[1])
            self.InputSize = int(received_obj[2])
            self.HiddenSize = int(received_obj[3])
            self.HiddenLayers = int(received_obj[4])
            self.OutputSize = int(received_obj[5])
            #世代を作る
            self.GenerateFirstPop()

        #世代交代
        elif (unityOrder == "GenerateNextPop") :
            #世代更新のためのパラメータを取得
            current_generation = int(received_obj[1])
            elite_selection = int(received_obj[2])
            tournament_selection = int(received_obj[3])
            
            #世代交代
            self.GenerateNextPop(current_generation, elite_selection, tournament_selection)

        #現世代のすべての個体が評価済みかどうかをチェック
        elif (unityOrder == "CheckFinishGeneration") :
            if (sum(self.waitFlag) != 0) :
                generation_text = "Wait"
            else :
                generation_txt = "GoNextGen"
        
        #評価を受け取れていない個体のIDをC#に報告する
        elif (unityOrder == "CheckLost") :
            lost_text = self.CheckLostID()

        #C#側から個体に関する評価を取得、対応するNNに紐付ける
        elif (unityOrder == "SetReward"):
            brainID = int(received_obj[1])
            self.Brains[brainID].Reward = float(received_obj[2])
            self.waitFlag[brainID] = 0
        
        #C#側から与えられた状態を元に次の行動を出力
        elif (unityOrder == "GetAction"):
            start_time = time.time()
            agentInfo_size = self.InputSize + 1
            for agentNum in range(int((len(received_obj)-1) / (agentInfo_size))):
                reading_position = agentNum * (agentInfo_size)
                #何番目のエージェントか
                brainID = int(received_obj[1 + reading_position])
                observations = []
                #そのエージェントの状態を取得
                for i in range(self.InputSize):
                    observations.append(float(received_obj[i + 2 + reading_position]))
                observations = torch.Tensor(observations)
                #NNに行動を計算させる
                actions = self.Brains[brainID](observations)
                #Cs側で受け取りやすいようにデータ整形
                action_txt += " " + str(brainID) + " " + str(actions.detach().numpy())
                action_txt = action_txt.replace("[","").replace("]","")
                while ("  " in action_txt) :
                    action_txt = action_txt.replace("  ", " ")
            elapsed_time = time.time() - start_time
        #ファイルからNNを読み込み
        elif(unityOrder == "LoadNN"):
            self.InputSize = int(received_obj[1])
            self.HiddenSize = int(received_obj[2])
            self.HiddenLayers = int(received_obj[3])
            self.OutputSize = int(received_obj[4])
            self.BattleBrain=PyNNBrain(self.InputSize, self.HiddenSize, self.HiddenLayers, self.OutputSize)
            self.BattleBrain.load_state_dict(torch.load('model_weight.pth'))
            action_txt="Load Complete"
        elif(unityOrder == "GetActionForBattle"):
            observations = []
            #そのエージェントの状態を取得
            for i in range(self.InputSize):
                observations.append(float(received_obj[i + 1]))
            observations = torch.Tensor(observations)
            actions = self.BattleBrain(observations)

            #整形
            action_txt += str(actions.detach().numpy())
            action_txt = action_txt.replace("[","").replace("]","")
            while ("  " in action_txt) :
                action_txt = action_txt.replace("  ", " ")

        else :
            print("Undefined Order From Unity C# Script!?",unityOrder)

        return action_txt, generation_txt, lost_text

def main():
    parser=argparse.ArgumentParser(description="port info")
    parser.add_argument('--src', type=int, default=50007,help='source port')
    parser.add_argument('--dst', type=int, default=50009,help='destination port')
    args=parser.parse_args()
    #Communicator
    communicator = PyNECommunicator()
    #Unityとの通信設定
    server = ParallelUDPServerAsync('127.0.0.1', args.src, 50008, args.dst, 50010, 50011)
    loop = asyncio.get_event_loop()
    loop.run_until_complete(server.read(communicator.receive))

if __name__ == "__main__":
    main()
