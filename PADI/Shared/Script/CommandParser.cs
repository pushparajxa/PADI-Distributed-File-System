using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared.Script {
    public delegate void SimpleCommandDelegate(string process);
    public delegate void CreateCommandDelegate(string process, string fileName, int nbDataServers, int readQuorum, int writeQuorum);
    public delegate void DeleteCommandDelegate(string process, string fileName);
    public delegate void OpenCommandDelegate(string process, string fileName);
    public delegate void CloseCommandDelegate(string process, string fileName);
    public delegate void ReadCommandDelegate(string process, int fileRegister, Semantics semantics, int contentRegister);
    public delegate void WriteRegisterCommandDelegate(string process, int fileRegister, int contentRegister);
    public delegate void WriteCommandDelegate(string process, int fileRegister, byte[] content);
    public delegate void CopyCommandDelegate(string process, int fileRegister1, Semantics semantics, int fileRegister2, byte[] salt);
    public delegate void ScriptCommandDelegate(string process, string fileName);

    //public enum ProcessType { Client, DataServer, MetadataServer };

    public class CommandParser {
        public SimpleCommandDelegate FailCommand;
        public SimpleCommandDelegate RecoverCommand;
        public SimpleCommandDelegate FreezeCommand;
        public SimpleCommandDelegate UnfreezeCommand;
        public CreateCommandDelegate CreateCommand;
        public DeleteCommandDelegate DeleteCommand;
        public OpenCommandDelegate OpenCommand;
        public CloseCommandDelegate CloseCommand;
        public ReadCommandDelegate ReadCommand;
        public WriteRegisterCommandDelegate WriteRegisterCommand;
        public WriteCommandDelegate WriteCommand;
        public CopyCommandDelegate CopyCommand;
        public SimpleCommandDelegate DumpCommand;
        public ScriptCommandDelegate ScriptCommand;


        private string[] lines;
        private int lineCounter;
        private static readonly char[] delimiters = { ' ', ',', '\t' };


        public CommandParser() {
            lines = null;
            lineCounter = 0;
        }

        public void LoadFile(string fileName) {
            if (fileName == null || fileName.Length == 0) throw new System.IO.FileNotFoundException();
            lines = System.IO.File.ReadAllLines(fileName);
            lineCounter = 0;
        }

        public void ExecuteNext() {
            if (lines == null) return;
            lineCounter++;
            ProcessCommand(lines[lineCounter - 1]);
        }

        public void ExecuteAll() {
            //lineCounter should continue normal execution without restart
            if (lines == null) return;
            while (lineCounter < lines.Length) ExecuteNext();
        }

        public void Restart() {
            lineCounter = 0;
        }

        private void ProcessCommand(string command) {
            if (command == null) return;
            char[] new_delimiters = {  ',' };
            string[] newParts = command.Split(new_delimiters);
            string[] parts = command.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            switch (parts[0].ToUpper()) {
                case "FAIL":
                    if (FailCommand != null) FailCommand(normalizeProcessId(parts[1]));
                    break;
                case "RECOVER":
                    if (RecoverCommand != null) RecoverCommand(normalizeProcessId(parts[1]));
                    break;
                case "FREEZE":
                    if (FreezeCommand != null) FreezeCommand(normalizeProcessId(parts[1]));
                    break;
                case "UNFREEZE":
                    if (UnfreezeCommand != null) UnfreezeCommand(normalizeProcessId(parts[1]));
                    break;
                case "CREATE":
                    if (CreateCommand != null) CreateCommand(normalizeProcessId(parts[1]), parts[2], int.Parse(parts[3]), int.Parse(parts[4]), int.Parse(parts[5]));
                    break;
                case "DELETE":
                    if (DeleteCommand != null) DeleteCommand(normalizeProcessId(parts[1]), parts[2]);
                    break;
                case "OPEN":
                    if (OpenCommand != null) OpenCommand(normalizeProcessId(parts[1]), parts[2]);
                    break;
                case "CLOSE":
                    if (CloseCommand != null) CloseCommand(normalizeProcessId(parts[1]), parts[2]);
                    break;
                case "READ":
                    if(ReadCommand != null) ReadCommand(normalizeProcessId(parts[1]), int.Parse(parts[2]), TranslateSemantics(parts[3]), int.Parse(parts[4]));
                    break;
                case "WRITE":
                    if(char.IsNumber(parts[3][0])) {
                        if(WriteRegisterCommand != null) WriteRegisterCommand(normalizeProcessId(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]));
                    } else {
                        if(WriteCommand != null) WriteCommand(normalizeProcessId(parts[1]), int.Parse(parts[2]), TranslateData(parts[3]));
                    }
                    break;
                case "COPY":
                    if(CopyCommand != null) CopyCommand(normalizeProcessId(parts[1]), int.Parse(parts[2]), TranslateSemantics(parts[3]), int.Parse(parts[4]), TranslateData(parts[5]));
                    break;
                case "DUMP":
                    if(DumpCommand != null) DumpCommand(normalizeProcessId(parts[1]));
                    break;
                case "EXESCRIPT":
                    if(ScriptCommand != null) ScriptCommand(normalizeProcessId(parts[1]), parts[2]);
                    break;
                case "#":
                    ExecuteNext();
                    break; //comments are ignored
                default:
                    //this is for unknown stuff, right now I just ignore
                    break;
            }
        }


        private string normalizeProcessId(string pid) {
            //this function may seem unnecessary but it makes it easier if we need to change the normalization
            return pid.ToLower();
        }


        /*
        private ProcessType GetProcessType(string process) {
            switch(process[0]) {
                case 'm':
                case 'M':
                    return ProcessType.MetadataServer;
                case 'd':
                case 'D':
                    return ProcessType.DataServer;
                case 'c':
                case 'C':
                    return ProcessType.Client;
                default:
                    throw new UnknownProcessTypeException();
            }
        }

        private int GetProcessId(string process) {
            return int.Parse(process.Substring(2));
        }
        */
        private Semantics TranslateSemantics(string semantics) {
            switch(semantics.ToUpper()) {
                case "DEFAULT":
                    return Semantics.DEFAULT;
                case "MONOTONIC":
                    return Semantics.MONOTONIC;
                default:
                    return Semantics.DEFAULT;
            }
        }

        private byte[] TranslateData(string data) {
            return Encoding.ASCII.GetBytes(data); //change encoding if problems arise
        }
    }

    public class UnknownProcessTypeException : Exception { }
}
