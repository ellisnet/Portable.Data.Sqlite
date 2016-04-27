using System;

namespace Portable.Data
{
    [Flags]
    public enum CommandBehavior
    {
        Default = 0,
        SingleResult = 1,
        SchemaOnly = 2,
        KeyInfo = 4,
        SingleRow = 8,
        SequentialAccess = 16,
        CloseConnection = 32,
    }
}
