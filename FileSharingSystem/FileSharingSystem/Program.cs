using FileSharingSystem.Data;
using FileSharingSystem.Data.Entities;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using WebSocketSharp;
using System.Net.WebSockets;
using System.Threading;
using System.Security.Cryptography;
using System.Text;
using System.Data;
using ConsoleTables;

namespace FileSharingSystem
{
    class Program
    {
        //DHT is distributed hash table which is synced with all nodes
        public static List<Node> DHT = new List<Node>();
        public static int Port = 0;
        public static ServerConnection Server = null;
        public static ClientPeer2Peer Client = new ClientPeer2Peer();
        public static string FileContent;
        public static string name = "Unkown";

        //define diffi-hellman parameters for having save communication between Peers
        public static int Prime;
        public static int PublicKey;
        public static int PrivateKey;
        public static bool IsActive = true;
        //the path that Demo files are put in. there are files that will be uploaded by nodes in the network
        public static string FilesPath = @"D:\DotNet Projects\FileSharingSystem\FileSharingSystem\Files\";

        public static void Log(string text)
        {
            Console.WriteLine(text);
        }


        //public static bool flag = true;
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Node nd = new Node();
            if (args.Length < 2)
            {
                Log("You should Enter 2 Parameters as Input");
            }

            if (args.Length >= 2)
            {
                Server = new ServerConnection(args[0], args[1]);
                Port = int.Parse(args[0]);

                //set the node properties
                nd.Url = args[0];
                nd.Name = args[1];
                nd.FileName = "No File Ownership";
                nd.HashValue = "nothing to show";
                nd.IsActive = true;
                DHT.Add(nd);

                // creating a websocket for this node 
                Server.Start();
            }

            if (name != "Unkown")
            {
                Log($"Current user is {name}");
            }

            // Log($"Launched from {Environment.CurrentDirectory}");
            int index;
            int selection = 0;
            while (selection != 8)
            {
                switch (selection)
                {
                    //=======================================================================================================
                    //=============  Connecting to another Peer in the network and add this Peer as its server ==============
                    //=======================================================================================================
                    case 1:
                        Log("Please enter the Peer URL");
                        string serverURL = Console.ReadLine();
                        string ServerPort = serverURL.Substring(15);
                        //index = DHT.FindIndex(item => item.Url == serverURL);
                        //if (index != -1 && DHT[index].IsActive == false)
                        //{
                        //    Log("this Node is Down");
                        //    break;
                        //}
                        //else
                        //{
                        Client.Connect($"{serverURL}/FileSharingSystem", string.Empty, false);
                        index = DHT.FindIndex(item => item.Url == Port.ToString());
                        DHT[index].ServerList = serverURL + "," + DHT[index].ServerList;
                        nd.ServerList = DHT[index].ServerList;
                        nd.IsActive = true;
                        DHT[index].IsActive = nd.IsActive;
                        IsActive = true;
                        Server.Brodcast(JsonConvert.SerializeObject(DHT));
                        Client.Broadcast(JsonConvert.SerializeObject(DHT));
                        Log("The Node Connected to the P2P Network");
                        break;
                    // }

                    //=======================================================================================================
                    //===========================  Allocate ownership of specific file to specific node =====================
                    //=======================================================================================================
                    case 2:
                        Log("Please enter the name of file");
                        string filename = Console.ReadLine();
                        index = DHT.FindIndex(item => item.FileName == filename);
                        if (index == -1)
                        {
                            index = DHT.FindIndex(item => item.Url == Port.ToString());
                            nd.FileName = filename;
                            DHT[index].FileName = filename;
                            FileContent = Crypto.LoadFile(FilesPath + filename);
                            nd.HashValue = nd.CalculateSha256HashValue(FileContent);
                            DHT[index].HashValue = nd.CalculateSha256HashValue(FileContent);
                            Server.Brodcast(JsonConvert.SerializeObject(DHT));
                            Client.Broadcast(JsonConvert.SerializeObject(DHT));
                            Log("The File Uploaded Successfully!");

                        }
                        else
                        {
                            Log("Sorry!!!! The ownership of this file belong to another node");
                        }
                        break;

                    //=======================================================================================================
                    //=======================================  Showing DHT in the Output  ===================================
                    //=======================================================================================================
                    case 3:
                        index = DHT.FindIndex(item => item.Url == Port.ToString());
                        //if (DHT[index].IsActive == false)
                        //Log("This is Down, DHT is not Updated ");
                        //else
                        //{
                        Log("");
                        Log("Distributed Hash Table:");
                        var DHT_display = new ConsoleTable("Peer Name", "Port", "File Ownership", "Hash Value of the File", "Is Avtive");
                        string status = "";

                        foreach (var node in DHT)
                        {
                            //if (nd.IsActive)
                            //    status = "Active";
                            //else
                            //    status = "Down";
                            DHT_display.AddRow(node.Name, node.Url, node.FileName, node.HashValue, node.IsActive);

                            // }
                            DHT_display.Write();
                            Console.WriteLine();
                        }
                        break;

                    //=======================================================================================================
                    //==================================  Down a Node from the P2P Network  =================================
                    //=======================================================================================================
                    case 4:
                        index = DHT.FindIndex(item => item.Url == Port.ToString());
                        DHT[index].IsActive = false;
                        IsActive = false;
                        //Client.CheckServerActiveStatus();
                        Server.Brodcast(JsonConvert.SerializeObject(DHT));
                        Client.Broadcast(JsonConvert.SerializeObject(DHT));

                        Log("The Node is Down Now");

                        //Server.wss.RemoveWebSocketService($"ws://127.0.0.1:{Port.ToString()}");
                        //Server.wss.Stop();
                        //Client.CheckServerActiveStatus();
                        //Server.wss.Stop();
                        var lst = DHT.Where(i => i.Url != Port.ToString()).ToList();
                        foreach (var item in lst)
                        {
                            DHT.Remove(item);
                        }

                        break;

                    //=======================================================================================================
                    //=============================  Requesting  file from other Peer in the network  =======================
                    //=======================================================================================================
                    case 5:
                        Log("Please Enter the FileName:");
                        string Target_file = Console.ReadLine();
                        index = DHT.FindIndex(item => item.FileName == Target_file);
                        if (index != -1)
                        {
                            serverURL = "ws://127.0.0.1:" + DHT[index].Url;
                            Prime = CyclicGroup.FindP();
                            PublicKey = Crypto.CalculatePublicKey(Prime, out PrivateKey);
                            nd.PublicKey = PublicKey;
                            RequestFile FileReq = new RequestFile();
                            FileReq.FileName = Target_file;
                            FileReq.RequestUrl = "ws://127.0.0.1:" + Port.ToString(); //the url that requests a file
                            FileReq.FileOwnerUrl = "ws://127.0.0.1:" + DHT[index].Url;//the url that owns the file
                            FileReq.PrimeNumber = Prime;
                            FileReq.PublicKey = nd.PublicKey;
                            //sending prime number and publicKey of this side to other the side 
                            Client.Connect($"{serverURL}/FileSharingSystem", JsonConvert.SerializeObject(FileReq), true);
                        }
                        else
                        {
                            Log("This file does not exist in the P2P Network System!!!!");
                        }
                        break;

                    //=======================================================================================================
                    //===================================  Action for showing P2P network  ==================================
                    //=======================================================================================================
                    case 6:
                        Log("============ Current P2P Network ===========");
                        Log("============================================");

                        Log("");
                        Log("Current P2P Network:");
                        var P2PNetwork = new ConsoleTable("Peer Name", "Port", "Active/Down", "ServersList");
                        string node_status;
                        foreach (var node in DHT)
                        {
                            if (node.IsActive == true)
                                node_status = "Active";
                            else node_status = "Down";
                            P2PNetwork.AddRow(node.Name, node.Url, node_status, node.ServerList);

                        }
                        P2PNetwork.Write();
                        Console.WriteLine();
                        break;

                    //=======================================================================================================
                    //================================   Action for showing all files in the network  =======================
                    //=======================================================================================================
                    case 7:
                        Log("================  All Files  ===============");
                        Log("============================================");

                        Log("All Files:");
                        var AllFiles = new ConsoleTable("File Name", "Port", "Accessibility");
                        string Accessibility;
                        foreach (var node in DHT)
                        {
                            if (node.FileName != "No File Ownership")
                            {
                                if (node.IsActive == true)
                                    Accessibility = "Accessible";
                                else
                                    Accessibility = "Not Accessible";

                                AllFiles.AddRow(node.FileName, node.Url, Accessibility);
                            }
                        }
                        AllFiles.Write();
                        Console.WriteLine();

                        break;
                }

                Log("============================================");
                Log($" =====   This is {nd.Name} on Port: {Port}  ====");
                Log("============================================");
                Log("1. Connect to a Peer in the Network");
                Log("2. Upload a file to System and Get the Ownership");
                Log("3. Display Current DHT:");
                Log("4. Down a Node");
                Log("5. Request a File");
                Log("6. Show the Peer2Peer Network");
                Log("7. Show All Files with their Accessibility");
                Log("Please select an Action:");
                Log("============================================");
                string action = Console.ReadLine();
                selection = int.Parse(action);
            }

            Client.Close();
        }
    }
}
