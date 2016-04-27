using System;

namespace Portable.Data
{
    public interface IDbCommand : IDisposable
    {
        string CommandText { get; set; }
        int CommandTimeout { get; set; }
        CommandType CommandType { get; set; }
        IDbConnection Connection { get; set; }
        IDataParameterCollection Parameters { get; }
        IDbTransaction Transaction { get; set; }
        UpdateRowSource UpdatedRowSource { get; set; }

        void Cancel();
        IDbDataParameter CreateParameter();
        int ExecuteNonQuery();
        long ExecuteReturnRowId();
        IDataReader ExecuteReader();
        IDataReader ExecuteReader(CommandBehavior behavior);
        object ExecuteScalar();
        void Prepare();
    }
}
