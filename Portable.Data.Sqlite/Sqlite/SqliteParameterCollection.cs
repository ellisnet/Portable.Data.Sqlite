// ReSharper disable InconsistentNaming

using System;
using System.Collections;
using System.Collections.Generic;

namespace Portable.Data.Sqlite
{
	public sealed class SqliteParameterCollection : IDataParameterCollection {

		internal SqliteParameterCollection()
		{
			m_parameters = new List<SqliteParameter>();
		}

		public SqliteParameter Add(string parameterName, DbType dbType)
		{
			var parameter = new SqliteParameter
			{
				ParameterName = parameterName,
				DbType = dbType,
			};
			m_parameters.Add(parameter);
			return parameter;
		}

		public int Add(object value)
		{
			m_parameters.Add((SqliteParameter) value);
			return m_parameters.Count - 1;
		}

		public void AddRange(Array values)
		{
			foreach (var obj in values)
				Add(obj);
		}

		public bool Contains(object value)
		{
			return m_parameters.Contains((SqliteParameter) value);
		}

		public bool Contains(string value)
		{
			return IndexOf(value) != -1;
		}

		public void CopyTo(Array array, int index)
		{
			throw new NotSupportedException();
		}

		public void Clear()
		{
			m_parameters.Clear();
		}

		public IEnumerator GetEnumerator()
		{
			return m_parameters.GetEnumerator();
		}

		private IDbDataParameter GetParameter(int index)
		{
			return m_parameters[index];
		}

		private IDbDataParameter GetParameter(string parameterName)
		{
			return m_parameters[IndexOf(parameterName)];
		}

		public int IndexOf(object value)
		{
			return m_parameters.IndexOf((SqliteParameter) value);
		}

		public int IndexOf(string parameterName)
		{
			return m_parameters.FindIndex(x => x.ParameterName == parameterName);
		}

		public void Insert(int index, object value)
		{
			m_parameters.Insert(index, (SqliteParameter) value);
		}

		public void Remove(object value)
		{
			m_parameters.Remove((SqliteParameter) value);
		}

		public void RemoveAt(int index)
		{
			m_parameters.RemoveAt(index);
		}

		public void RemoveAt(string parameterName)
		{
			RemoveAt(IndexOf(parameterName));
		}

		private void SetParameter(int index, IDbDataParameter value)
		{
			m_parameters[index] = (SqliteParameter) value;
		}

		private void SetParameter(string parameterName, IDbDataParameter value)
		{
			SetParameter(IndexOf(parameterName), value);
		}

		public int Count
		{
			get { return m_parameters.Count; }
		}

		public bool IsFixedSize
		{
			get { return false; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool IsSynchronized
		{
			get { return false; }
		}

		public object SyncRoot
		{
			get { throw new NotSupportedException(); }
		}

		public SqliteParameter this[int index]
		{
			get { return m_parameters[index]; }
			set { m_parameters[index] = value; }
		}

		readonly List<SqliteParameter> m_parameters;

        object IList.this[int index] {
            get { return this.GetParameter(index); }
            set { this.SetParameter(index, (SqliteParameter)value); }
        }

        object IDataParameterCollection.this[string parameterName] {
            get { return this.GetParameter(parameterName); }
            set { this.SetParameter(parameterName, (SqliteParameter)value); }
        }
    }
}
