using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared.Services.Request {
    public class MetadataRequestService : MarshalByRefObject {
        public event OpenRequestHandler OpenHandler;
        public event CloseRequestHandler CloseHandler;
        public event CreateRequestHandler CreateHandler;
        public event DeleteRequestHandler DeleteHandler;
        public event SendCreatePromiseRequestHandler SendCreatePromiseHandler;
        public event SendCreateAcceptedRequestHandler SendCreateAcceptedHandler;
        public event CreateCommitRequestHandler CreateCommitHandler;

        public MetadataDTO Open(string fileName) {
            if (OpenHandler != null) {
                return OpenHandler(fileName);
            }
            else return null; //is there a more proper way to report this failure?
        }

        public void Close(string filename) {
            if (CloseHandler != null) CloseHandler(filename);
        }

        public MetadataDTO Create(string fileName, int nbDataServers, int readQuorum, int writeQuorum) {
            if (CreateHandler != null) {
                return CreateHandler(fileName, nbDataServers, readQuorum, writeQuorum);
            }
            else return null; //is there a more proper way to report this failure?
        }

        public MetadataQuorumDto SendCreatePromise(int nbDataServers, int round)
        {
            if (SendCreatePromiseHandler != null)
            {
                return SendCreatePromiseHandler(nbDataServers, round);
            }
            else throw new System.InvalidOperationException("SendCreatePromiseHandler instance in MetadataRequestService doesn't exists!");
        }

        public bool SendCreateAccepted(MetadataQuorumDto dataservers, int nbDataServers) {
            if (SendCreateAcceptedHandler != null)
            {
                return SendCreateAcceptedHandler(dataservers, nbDataServers);
            }
            else throw new System.InvalidOperationException("SendCreatePromiseHandler instance in MetadataRequestService doesn't exists!");
        }

        public MetadataDTO CreateCommit(MetadataQuorumDto dataservers, string fileName, int nbDataServers, int readQuorum, int writeQuorum)
        {
            if (CreateCommitHandler != null)
            {
                return CreateCommitHandler(dataservers, fileName, nbDataServers, readQuorum, writeQuorum);
            }
            else throw new System.InvalidOperationException("CreateCommitHandler instance in MetadataRequestService doesn't exists!");
        }


        public void Delete(string filename) {
            if (DeleteHandler != null) DeleteHandler(filename);
        }
    }

    public delegate MetadataDTO OpenRequestHandler(String fileName);
    public delegate void CloseRequestHandler(String fileName);
    public delegate MetadataDTO CreateRequestHandler(string fileName, int nbDataServers, int readQuorum, int writeQuorum);
    public delegate void DeleteRequestHandler(String fileName);
    public delegate MetadataQuorumDto SendCreatePromiseRequestHandler(int nbDataServers, int round);
    public delegate bool SendCreateAcceptedRequestHandler(MetadataQuorumDto dataservers, int nbDataServers);
    public delegate MetadataDTO CreateCommitRequestHandler(MetadataQuorumDto dataservers, string fileName, int nbDataServers, int readQuorum, int writeQuorum);
}
