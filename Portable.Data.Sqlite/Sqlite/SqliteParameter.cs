using System;

namespace Portable.Data.Sqlite
{
	public sealed class SqliteParameter : IDbDataParameter {

		public DbType DbType { get; set; }

		public ParameterDirection Direction { get; set; } = ParameterDirection.Input; //Defaulting to input

		public bool IsNullable { get; set; }

		public string ParameterName { get; set; }

		public int Size { get; set; }

		public string SourceColumn
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		public bool SourceColumnNullMapping
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		public DataRowVersion SourceVersion
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		public object Value { get; set; }

		public void ResetDbType()
		{
			DbType = default(DbType);
		}

	    public SqliteParameter() { }

	    public SqliteParameter(string parameterName, object value) {
	        ParameterName = parameterName;
	        Value = value;
	    }

        public byte Precision {
            get { return 0; }
            set { }
        }

        public byte Scale {
            get { return 0; }
            set { }
        }
    }
}
