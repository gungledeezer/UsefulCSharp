// Useful C#
// Copyright (C) 2014-2020 Nicholas Randal
// 
// Useful C# is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

using System;
using Randal.Logging;

namespace Randal.Sql.Deployer.App
{
	public interface IRunnerSettings
	{
		IRollingFileSettings FileLoggerSettings { get; }

		string ScriptProjectFolder { get; }

		string Server { get; }

		bool HasUnifiedScriptPath { get; }

		string UnifiedScriptPath { get; }

		bool NoTransaction { get; }

		bool UseTransaction { get; }

		bool ShouldRollback { get; }

		bool CheckFilesOnly { get; }

		bool BypassCheck { get; }
	}

	public sealed class RunnerSettings : IRunnerSettings
	{
		public RunnerSettings(
			string scriptProjectFolder, 
			string logFolder, 
			string server, 
			string unifiedScriptPath,
			bool rollback = false, 
			bool noTransaction = false, 
			bool checkFilesOnly = false, 
			bool bypassCheck = false)
		{
			if(checkFilesOnly && bypassCheck)
				throw new ArgumentException("bypassCheck and checkFilesOnly cannot both be true.", nameof(bypassCheck));

			if(checkFilesOnly && (noTransaction || rollback == false))
				throw new ArgumentException("When 'checkFilesOnly' is True, then 'noTran' must be False and 'rollback' must be True.", nameof(checkFilesOnly));

			ScriptProjectFolder = scriptProjectFolder;
			Server = server;
			UnifiedScriptPath = unifiedScriptPath;
			NoTransaction = noTransaction;
			ShouldRollback = rollback;
			CheckFilesOnly = checkFilesOnly;
			BypassCheck = bypassCheck;

			FileLoggerSettings = new RollingFileSettings(logFolder, "SqlScriptDeployer");
		}

		public IRollingFileSettings FileLoggerSettings { get; }

		public string ScriptProjectFolder { get; }

		public string Server { get; }

		public string UnifiedScriptPath { get; }

		public bool HasUnifiedScriptPath => string.IsNullOrWhiteSpace(UnifiedScriptPath) == false;

		public bool NoTransaction { get; }

		public bool UseTransaction => NoTransaction == false;

		public bool ShouldRollback { get; }

		public bool CheckFilesOnly { get; }

		public bool BypassCheck { get; }
	}
}