using System;
using System.Threading;
using System.Threading.Tasks;

namespace Portable.Data
{
    public interface IDataReader : IDisposable, IDataRecord
    {
        int Depth { get; }
        bool IsClosed { get; }
        int RecordsAffected { get; }

        void Close();
		DataTable GetSchemaTable();
        bool NextResult();
        bool Read();

        Task<bool> ReadAsync(CancellationToken cancellationToken);

        Task<bool> NextResultAsync(CancellationToken cancellationToken);
    }
}
