using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Shared.Services.Command {
    /* How this one works:
     * Instead of having a specific Command Module for each component type,
     * it seemed more reasonable and flexible to have all components use
     * a generic command module. This allows easy modifications to the behaviour
     * of each component without having to change anything on the puppet master.
     * One of the benefits of this is that we can easily implement some of the 
     * features on other components for debug purposes (for example, Dump).
     * It also allows enough flexibility to treat all components equally on the 
     * puppet master's side, in case no specialized operations are needed.
     * Each component will have it's own instance of this service class, setting 
     * the handlers only to the services they make available.
     * Unavailable operations will simply produce no effect, instead of having to 
     * handle errors or halting the system.
     * All components except the puppet master should have this service available.
     */

    public delegate void SimpleCommandHandler();
    public delegate void CreateCommandHandler(string fileName, int nbDataServers, int readQuorum, int writeQuorum);
    public delegate void OpenCommandHandler(string fileName);
    public delegate void DeleteCommandHandler(string fileName);
    public delegate void CloseCommandHandler(string fileName);
    public delegate void ReadCommandHandler(int fileRegister, Semantics semantics, int contentRegister);
    public delegate void WriteCommandHandler(int fileRegister, byte[] content);
    public delegate void WriteRegisterCommandHandler(int fileRegister, int contentRegister);
    public delegate void CopyCommandHandler(int fileRegister1, Semantics semantics, int fileRegister2, byte[] salt);
    public delegate void ScriptCommandHandler(string fileName);
    public delegate string DumpCommandHandler();


    public class CommandService : MarshalByRefObject {
        public event SimpleCommandHandler FailHandler;
        public event SimpleCommandHandler RecoverHandler;
        public event SimpleCommandHandler FreezeHandler;
        public event SimpleCommandHandler UnfreezeHandler;
        public event CreateCommandHandler CreateHandler;
        public event DeleteCommandHandler DeleteHandler;
        public event OpenCommandHandler OpenHandler;
        public event CloseCommandHandler CloseHandler;
        public event ReadCommandHandler ReadHandler;
        public event WriteCommandHandler WriteHandler;
        public event WriteRegisterCommandHandler WriteRegisterHandler;
        public event CopyCommandHandler CopyHandler;
        public event DumpCommandHandler DumpHandler;
        public event ScriptCommandHandler ScriptHandler;

        /* temporarily disconnects a process from the network, disabling its
         * ability to send to or receive messages from other processes */
        public void Fail() {
            if (FailHandler != null) FailHandler();
        }

        /* opposite of the above command */
        public void Recover() {
            if (RecoverHandler != null) RecoverHandler();
        }

        /* similar to fail but the messages are buffered */
        public void Freeze() {
            if (FreezeHandler != null) FreezeHandler();
        }

        /* similar to recover, but the target processes buffered nessages 
         * before resuming normal operation */
        public void Unfreeze() {
            if (UnfreezeHandler != null) UnfreezeHandler();
        }

        /* instructs the creation of a file with the corresponding metadata */
        public void Create(string fileName, int nbDataServers, int readQuorum, int writeQuorum) {
            if (CreateHandler != null) CreateHandler(fileName, nbDataServers, readQuorum, writeQuorum);
        }

        /* deletes the file */
        public void Delete(string fileName) {
            if (DeleteHandler != null) DeleteHandler(fileName);
        }

        /* open the desired file */
        public void Open(string fileName) {
            if (OpenHandler != null) OpenHandler(fileName);
        }

        /* instructs a client to close a file that it has previously opened */
        public void Close(string fileName) {
            if (CloseHandler != null) CloseHandler(fileName);
        }

        /* reads the contents of the file identified by a file register 
         * and stores it in a string register in the Puppet Master */
        public void Read(int fileRegister, Semantics semantics, int contentRegister) {
            if (ReadHandler != null) ReadHandler(fileRegister, semantics, contentRegister);
        }

        /* writes the file identified by a file register with the contents 
         * previously stored in a string register */
        public void WriteRegister(int fileRegister, int contentRegister) {
            if (WriteHandler != null) WriteRegisterHandler(fileRegister, contentRegister);
        }

        /* writes the file identified by a file register with the contents
         *  provided in the command to the Puppet Master */
        public void Write(int fileRegister, byte[] content) {
            if (WriteHandler != null) WriteHandler(fileRegister, content);
        }

        /* reads the content of file whose metadata is stored in 
         * file-register1, and writes as new contents of the file with
         * metadata in file-register2, the concatenation of the content of 
         * the first file with a string (to be stored as a byte array), 
         * that serves as salt to make them slightly different */
        public void Copy(int fileRegister1, Semantics semantics, int fileRegister2, byte[] salt) {
            if (CopyHandler != null) CopyHandler(fileRegister1, semantics, fileRegister2, salt);
        }

        /* prints all the values stored at the metadata servers, including 
         * for each file, its metadata, and the data servers holding 
         * replicas of the file, with the corresponding local filenames */
        public string Dump() {
            return DumpHandler != null ? DumpHandler() : null;
        }

        /* instructs a given client to start executing all the commands 
         * included in another script file named "filename", assumed to 
         * be locally available */
        public void ExeScript(string fileName) {
            if (ScriptHandler != null) ScriptHandler(fileName);
        }
    }
}
