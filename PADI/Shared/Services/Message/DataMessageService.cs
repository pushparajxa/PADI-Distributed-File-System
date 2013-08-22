using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared.Services.Message {
    public class DataMessageService : MarshalByRefObject {
        public event CreateNewFileMessageHandler CreateNewFileHandler;
        public event DeleteFileMessageHandler DeleteFileHandler;
        public event CopyFileMessageHandler CopyFileHandler;
        public event PasteFileMessageHandler PasteFileHandler;

        //Requests the dataServer to create a new file. DS replies with the localFilename.
        public string CreateNewFile() {
            if (CreateNewFileHandler != null) {
                return CreateNewFileHandler();
            }
            else return null; //is there a more proper way to report this failure?
        }

        //Deletes file
        public void DeleteFile(string localFileName) {
            if (DeleteFileHandler != null) DeleteFileHandler(localFileName);
        }

        //Gets file
        public DataDTO CopyFile(string localFileName) {
            if (CopyFileHandler != null) {
                return CopyFileHandler(localFileName);
            }
            else return null;
        }

        //Creates replica of file, returns localFileName
        public string PasteFile(DataDTO file) {
            if (PasteFileHandler != null) {
                return PasteFileHandler(file);
            }
            else return null;
        }
    }

    public delegate string CreateNewFileMessageHandler();
    public delegate void DeleteFileMessageHandler(string localFileName);
    public delegate DataDTO CopyFileMessageHandler(string localFileName);
    public delegate string PasteFileMessageHandler(DataDTO file);
}
