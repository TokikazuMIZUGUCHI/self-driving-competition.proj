# -*- coding: utf-8 -*-
import numpy as np
import torch
import torch.nn as nn
import torch.nn.functional as F

class PyNNBrain(nn.Module):
    #Layer情報の定義
    def __init__(self, inputSize, hiddenSize, hiddenLayers, outputSize):
        super(PyNNBrain, self).__init__()
        #ニューラルネットのパラメータを設定
        self.inputSize = inputSize
        self.hiddenSize = hiddenSize
        self.hiddenLayers = hiddenLayers
        self.outputSize = outputSize
        
        #全結合層の準備
        self.weight_params=nn.ParameterList([])
        self.bias_params=nn.ParameterList([])
        if (hiddenLayers != 0) :
            self.weight_params.append(nn.Parameter(torch.zeros(inputSize, hiddenSize)))
            self.bias_params.append(nn.Parameter(torch.ones(1, hiddenSize)))
            for i in range(hiddenLayers-1) :
                self.weight_params.append(nn.Parameter(torch.zeros(hiddenSize, hiddenSize)))
                self.bias_params.append(nn.Parameter(torch.ones(1, hiddenSize)))
            self.weight_params.append(nn.Parameter(torch.zeros(hiddenSize, outputSize)))
            self.bias_params.append(nn.Parameter(torch.ones(1, outputSize)))
        else:
            self.weight_params.append(nn.Parameter(torch.zeros(inputSize, outputSize)))
            self.bias_params.append(nn.Parameter(torch.ones(1, outputSize)))
        
        #重みの初期化
        for i in range(len(self.weight_params)):
            nn.init.uniform_(self.weight_params[i], -1.0, 1.0)
            nn.init.uniform_(self.bias_params[i], -1.0, 1.0)
        
        #報酬の初期化
        self.Reward = -9999
    
    #順伝播の計算方法を記述
    def forward(self, x):
        #活性化関数 Tanh
        for i in range(self.hiddenLayers):
            x = torch.tanh(torch.mm(x.reshape(1,-1), self.weight_params[i]) + self.bias_params[i])
        #最終層は活性化関数なし
        x = torch.mm(x,self.weight_params[self.hiddenLayers]) + self.bias_params[self.hiddenLayers]
        return x

    def MutateLayers(self, params, generation=0):
        #世代に応じて突然変異の強度を調整
        mutRate = 0.1 + self.MutRate(generation)
        mutSize = 0.02 + self.MutRate(generation) * 0.2
        for layer_num,layer_param in enumerate(params):
            layer_shape = layer_param.shape
            random_value = torch.rand(layer_shape)
            x_value = torch.rand(layer_shape)
            y_value = torch.rand(layer_shape)
            z1_value = torch.sqrt(-2 * torch.log(x_value)) * torch.cos(2 * np.pi * y_value)
            layer_param = torch.where(random_value < mutRate, layer_param+z1_value*mutSize, layer_param)
            layer_param = torch.where(random_value < mutRate * 0.05, z1_value, layer_param)
            params[layer_num] = nn.Parameter(layer_param)
        
    #突然変異操作
    def Mutate(self, generation=0):
        #各パラメータに突然変異操作の適用
        self.MutateLayers(self.weight_params, generation) 
        self.MutateLayers(self.bias_params, generation) 
        return self
    #突然変異の強さを世代数によって調整
    def MutRate(self, generation):
        return 0.2 * max(0, (1.0 - generation / 100.0))
   
    #PyNNBrain同士の優劣比較
    def __lt__(self, other) :
        return self.Reward < other.Reward
