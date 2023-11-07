using System.Diagnostics;
using System.IO;
using System;
using UnityEngine;

public class UDPCommunicator : MonoBehaviour
{
    [Header("UDP Settings"), SerializeField]
    private string ip = "127.0.0.1";
    private string IP => ip;

    [SerializeField] private int srcPort = 50008;
    private int SrcPort => srcPort;

    [SerializeField] private int dstPort = 50007;
    private int DstPort => dstPort;

    // [Header("Python Env"), SerializeField] private string pythonPath = string.Empty;
    // private string PythonPath => pythonPath;

    // [SerializeField] private string codePath = string.Empty;
    // private string CodePath => codePath;

    private UDP Client { get; } = new UDP();

    //private Process PythonProcess { get; } = new Process();

    private void Start() {
        /*
        PythonProcess.StartInfo = new ProcessStartInfo() {
            FileName = PythonPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            Arguments = $"{CodePath} {IP} {DstPort} {SrcPort}",
        };

        PythonProcess.Start();
        */
        Client.Set(IP, SrcPort, DstPort);
    }

    public void Send(string data) {
        Client.Send(data, _ => {
        });
    }

    private void OnDestroy() {
        Client.Dispose();
        // PythonProcess?.Close();
        // PythonProcess?.Dispose();
    }
}
