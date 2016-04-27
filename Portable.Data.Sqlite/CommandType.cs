using System;

namespace Portable.Data
{
    public enum CommandType
    {
        Text = 1,
        StoredProcedure = 4,
        TableDirect = 512,
    }
}
