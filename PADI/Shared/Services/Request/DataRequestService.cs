using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared.Services.Request {
    public class DataRequestService : MarshalByRefObject {
        public event ReadRequestHandler ReadHandler;
        public event WriteRequestHandler WriteHandler;
        public event WriteableRequestHandler WriteableHandler;
        public event ReadableRequestHandler ReadableHandler;
        public event VersionRequestHandler VersionHandler;
        public event CancelRequestHandler CancelHandler;

        //Read - Returns version and content
        public DataDTO Read(string localFileName, Semantics semantics) {
            if (ReadHandler != null) {
                return ReadHandler(localFileName, semantics);
            }
            else return null;  //is there a more proper way to report this failure?
        }

        //Write - void
        public void Write(string localFileName, byte[] content, int version, int tieBreaker) {
            if (WriteHandler != null) WriteHandler(localFileName, content, version, tieBreaker);
        }

        //returns the server's version of the specified file
        public int GetVersion(string localFileName) {
            if (ReadHandler != null) return VersionHandler(localFileName);
            else return -2;
        }

        //IsWriteable - Returns wether the server is available to write specified file
        public int IsWriteable(string localFileName, int version, int tieBreaker) {
            if (WriteableHandler != null) return WriteableHandler(localFileName, version, tieBreaker);
            else return -1;
        }

        //IsWriteable - Returns wether the server is available to read specified file
        public int IsReadable(string localFileName, int version) {
            if (ReadableHandler != null) return ReadableHandler(localFileName, version);
            else return -1;
        }

        public void Cancel(string localFileName, int sessionid) {
            if (ReadableHandler != null) CancelHandler(localFileName, sessionid);
        }
    }


    public delegate DataDTO ReadRequestHandler(string localFileName, Semantics semantics);
    public delegate void WriteRequestHandler(string localFileName, byte[] content, int version, int sessionid);
    public delegate int WriteableRequestHandler(string localFileName, int version, int tieBreaker);
    public delegate void CancelRequestHandler(string localFileName, int sessionid);
    public delegate int ReadableRequestHandler(string localFileName, int version);
    public delegate int VersionRequestHandler(string localFileName);
}
