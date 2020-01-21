using System.Collections.Generic;

namespace Randal.Sql.Deployer.Process
{
	public sealed class DummySqlConnectionManager : ISqlConnectionManager
	{
		public void Dispose() { }

		public string Server => "localhost";

		public IReadOnlyList<string> DatabaseNames { get; } = new List<string>();

		public void OpenConnection(string newServer, string database, string userName, string password) { }

		public void OpenConnection(string newServer, string database) { }

		public void BeginTransaction() { }

		public void CommitTransaction() { }

		public void RollbackTransaction() { }

		public ISqlCommandWrapper CreateCommand(string raw, params object[] values) => null;
	}
}