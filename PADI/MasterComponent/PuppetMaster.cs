using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
//using System.Runtime.Remoting.Channels.Tcp;
using Shared;
using Shared.Script;
using Shared.Services;
using Shared.Services.Command;
using Shared.Services.Request;
using Shared.Services.Message;


namespace MasterComponent {
    public delegate void OutputHandler(string ouput);


    public class PuppetMaster {
        private static readonly int BASEMETAPORT = 6060;
        private static readonly int BASEDATAPORT = 7070;
        private static readonly int BASECLIENTPORT = 5050;

        private static readonly int DEFAULT_NUM_MDS = 0;
        private static readonly int DEFAULT_NUM_DS = 0;
        private static readonly int DEFAULT_NUM_C = 0;

        private Dictionary<string, CommandService> processes;
        
        private int nextID_MDS;
        private int nextID_DS;
        private int nextID_C;

        private string log;


        public PuppetMaster(CommandParser parser) {
            nextID_MDS= 0;
            nextID_DS = 0;
            nextID_C = 0;
            processes = new Dictionary<string, CommandService>();

            parser.FailCommand += Fail;
            parser.RecoverCommand += Recover;
            parser.FreezeCommand += Freeze;
            parser.UnfreezeCommand += Unfreeze;
            parser.DumpCommand += Dump;
            parser.CreateCommand += Create;
            parser.DeleteCommand += Delete;
            parser.OpenCommand += Open;
            parser.CloseCommand += Close;
            parser.ReadCommand += Read;
            parser.WriteRegisterCommand += WriteRegister;
            parser.WriteCommand += Write;
            parser.CopyCommand += Copy;
            parser.ScriptCommand += ExeScript;

            log = "";

            Populate(DEFAULT_NUM_C, DEFAULT_NUM_DS, DEFAULT_NUM_MDS);
        }





        private void Populate(int nClients, int nData, int nMeta) {
            int i;
            for (i = 0; i < nMeta; i++) LaunchMetadataServer();
            for (i = 0; i < nData; i++) LaunchDataServer();
            for (i = 0; i < nClients; i++) LaunchClient();
        }


        public void LaunchMetadataServer() {
            while(processes.ContainsKey("m-" + nextID_MDS)) nextID_MDS++;
            LaunchMetadataServer(nextID_MDS);
        }

        public void LaunchDataServer() {
            while (processes.ContainsKey("d-" + nextID_DS)) nextID_DS++;
            LaunchDataServer(nextID_DS);
        }

        public void LaunchClient() {
            while (processes.ContainsKey("c-" + nextID_C)) nextID_C++;
            LaunchClient(nextID_C);
        }


        public void LaunchMetadataServer(int id) {
            string port = (BASEMETAPORT + id).ToString();
            string metadataPort = (BASEMETAPORT + id).ToString();
            Process.Start(new ProcessStartInfo("MetadataComponent.exe", port + " " + (BASEMETAPORT) + " " + metadataPort));
            processes.Add("m-" + id, (CommandService)Activator.GetObject(typeof(CommandService), "tcp://localhost:" + port + "/CommandService"));
            UpdateLog("Metadata server [m-" + id + "] launched.");
        }

        public void LaunchDataServer(int id) {
            string port = (BASEDATAPORT + id).ToString();
            Process.Start(new ProcessStartInfo("DataComponent.exe", port + " " + BASEMETAPORT));
            processes.Add("d-" + id, (CommandService) Activator.GetObject(typeof(CommandService), "tcp://localhost:" + port + "/CommandService"));
            UpdateLog("Data server [d-" + id + "] launched.");
        }

        public void LaunchClient(int id) {
            string port = (BASECLIENTPORT + id).ToString();
            Process.Start(new ProcessStartInfo("ClientComponent.exe", port + " " + BASEMETAPORT));
            processes.Add("c-" + id, (CommandService)Activator.GetObject(typeof(CommandService), "tcp://localhost:" + port + "/CommandService"));
            UpdateLog("Client [c-" + id + "] launched.");
        }


        public void LaunchProcessIfNotPresent(string process) {
            if (!processes.ContainsKey(process)) {
                int id = int.Parse(process.Substring(2));
                switch (process[0]) {
                    case 'm':
                        LaunchMetadataServer(id);
                        break;
                    case 'd':
                        LaunchDataServer(id);
                        break;
                    case 'c':
                        LaunchClient(id);
                        break;
                }
            }
        }


        public OutputHandler LogListener; //besides updating the GUI, can also be used to write the log to a file

        private void UpdateLog(string news) {
            log += news;
            if (LogListener != null) LogListener(log);
        }
        


        public void Fail(string process) {
            LaunchProcessIfNotPresent(process);
            processes[process].Fail();
        }

        public void Recover(string process) {
            LaunchProcessIfNotPresent(process);
            processes[process].Recover();
        }

        public void Freeze(string process) {
            LaunchProcessIfNotPresent(process);
            processes[process].Freeze();
        }

        public void Unfreeze(string process) {
            LaunchProcessIfNotPresent(process);
            processes[process].Unfreeze();
        }

        public void Dump(string process) {
            LaunchProcessIfNotPresent(process);
            UpdateLog("[" + process + "] dump: " + processes[process].Dump());
        }

        public void Create(string process, string fileName, int nbDataServers, int readQuorum, int writeQuorum) {
            LaunchProcessIfNotPresent(process);
            processes[process].Create(fileName, nbDataServers, readQuorum, writeQuorum);
        }

        public void Delete(string process, string fileName) {
            LaunchProcessIfNotPresent(process);
            processes[process].Delete(fileName);
        }

        public void Open(string process, string fileName) {
            LaunchProcessIfNotPresent(process);
            processes[process].Open(fileName);
        }

        public void Close(string process, string fileName) {
            LaunchProcessIfNotPresent(process);
            processes[process].Close(fileName);
        }

        public void Read(string process, int fileRegister, Semantics semantics, int contentRegister) {
            LaunchProcessIfNotPresent(process);
            processes[process].Read(fileRegister, semantics, contentRegister);
        }

        public void WriteRegister(string process, int fileRegister, int contentRegister) {
            LaunchProcessIfNotPresent(process);
            processes[process].WriteRegister(fileRegister, contentRegister);
        }

        public void Write(string process, int fileRegister, byte[] content) {
            LaunchProcessIfNotPresent(process);
            processes[process].Write(fileRegister, content);
        }

        public void Copy(string process, int fileRegister1, Semantics semantics, int fileRegister2, byte[] salt) {
            LaunchProcessIfNotPresent(process);
            processes[process].Copy(fileRegister1, semantics, fileRegister2, salt);
        }

        public void ExeScript(string process, string fileName) {
            LaunchProcessIfNotPresent(process);
            processes[process].ExeScript(fileName);
        }


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            CommandParser parser = new CommandParser();
            PuppetMaster master = new PuppetMaster(parser);
            Puppeteer puppy = new Puppeteer(master, parser);
            master.UpdateLog("Hello. I'm the Puppet Master. I would like to play a game.");
                        
            Application.Run(puppy);

        }
    }
}
