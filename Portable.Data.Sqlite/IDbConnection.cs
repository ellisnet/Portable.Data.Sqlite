using System;

namespace Portable.Data
{
    public interface IDbConnection : IDisposable
    {
        string ConnectionString { get; set; }
        
        int ConnectionTimeout { get; }

        string Database { get; }

        ConnectionState State { get; }

        IDbTransaction BeginTransaction();
        IDbTransaction BeginTransaction(IsolationLevel il);

        void ChangeDatabase(string databaseName);
        void Close();
        void SafeClose();

        IDbCommand CreateCommand();
        void Open();
        void SafeOpen();
    }
}
