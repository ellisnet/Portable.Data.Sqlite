// ReSharper disable InconsistentNaming

using System;

namespace Portable.Data
{
    public interface IDataRecord
    {
        int FieldCount { get; }
        object this[string name] { get; }
        object this[int i] { get; }
        bool GetBoolean(int i);
        bool GetBoolean(string name);
        byte GetByte(int i);
        byte GetByte(string name);
        long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length);
        long GetBytes(string name, long fieldOffset, byte[] buffer, int bufferoffset, int length);
        char GetChar(int i);
        char GetChar(string name);
        //long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length);
        //long GetChars(string name, long fieldoffset, char[] buffer, int bufferoffset, int length);
        IDataReader GetData(int i);
        IDataReader GetData(string name);
        string GetDataTypeName(int i);
        DateTime GetDateTime(int i);
        DateTime GetDateTime(string name);
        decimal GetDecimal(int i);
        decimal GetDecimal(string name);
        double GetDouble(int i);
        double GetDouble(string name);
        Type GetFieldType(int i);
        float GetFloat(int i);
        float GetFloat(string name);
        Guid GetGuid(int i);
        Guid GetGuid(string name);
        short GetInt16(int i);
        short GetInt16(string name);
        int GetInt32(int i);
        int GetInt32(string name);
        long GetInt64(int i);
        long GetInt64(string name);
        string GetName(int i);
        int GetOrdinal(string name);
        string GetString(int i);
        string GetString(string name);
        object GetValue(int i);
        object GetValue(string name);
        bool IsDBNull(int i);
        bool IsDBNull(string name);
        int GetValues(object[] values);
        long LastInsertRowId { get; }
        bool SetRowId { get; set; }

    }
}
