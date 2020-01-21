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
using System.IO;
using Newtonsoft.Json;
using Randal.Core.Enums;
using Randal.Logging;
using Randal.Sql.Deployer.Configuration;
using Randal.Sql.Deployer.IO;
using Randal.Sql.Deployer.Process;
using Randal.Sql.Deployer.Scripts;
using Randal.Sql.Deployer.Scripts.Blocks;

namespace Randal.Sql.Deployer.App
{
	public sealed class Runner
	{
		public Runner(IRunnerSettings settings, ILogger logger = null)
		{
			_settings = settings ?? throw new ArgumentNullException(nameof(settings));
			_logger = logger ?? new NullLogger();
			var configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");

			using (var reader = new FileInfo(configPath).OpenText())
				_config = JsonConvert.DeserializeObject<ScriptDeployerConfig>(reader.ReadToEnd());
		}

		public RunnerResolution Go()
		{
			var commit = false;

			using (var connectionManager = CreateSqlConnectionManager())
			{
				RunnerResolution resolution;

				try
				{
					LogOptions();

					_logger.PostEntry("opening connection to server", _settings.Server);
					connectionManager.OpenConnection(_settings.Server, "master");

					if (_settings.UseTransaction)
					{
						_logger.PostEntry("BEGINNING TRANSACTION");
						connectionManager.BeginTransaction();
					}

					var checker = _settings.BypassCheck ? null : CreateChecker();
					var project = LoadProject(CreateParser(), checker);
					if (_settings.CheckFilesOnly)
						return RunnerResolution.ValidationOnly;

					DeployScripts(_settings, _config, project, connectionManager);

					commit = _settings.ShouldRollback == false;
				}
				catch (RunnerException rex)
				{
					_logger.PostEntryNoTimestamp(string.Empty);
					_logger.PostEntry("<<< ERROR >>> {0}", rex.Message);
					return rex.RunnerResolution;
				}
				catch (Exception ex)
				{
					_logger.PostException(ex);
					return RunnerResolution.ExceptionThrown;
				}
				finally
				{
					resolution = ResolveTransaction(commit, connectionManager);
				}

				return resolution;
			}
		}

		private ISqlConnectionManager CreateSqlConnectionManager()
		{
			return _settings.HasUnifiedScriptPath 
				? (ISqlConnectionManager)new DummySqlConnectionManager() 
				: new SqlConnectionManager(_config.DatabaseLookup);
		}

		private RunnerResolution ResolveTransaction(bool commit, ISqlConnectionManager connectionManager)
		{
			_logger.PostEntryNoTimestamp(string.Empty);

			if (commit)
			{
				_logger.PostEntry(Verbosity.Vital, "~~~~~~~~~~ COMMITTING ~~~~~~~~~~{0}", Environment.NewLine);
				connectionManager.CommitTransaction();
				return RunnerResolution.Committed;
			}

			_logger.PostEntry(Verbosity.Vital, "~~~~~~~~~~ ROLLING BACK ~~~~~~~~~~{0}", Environment.NewLine);
			connectionManager.RollbackTransaction();
			return RunnerResolution.RolledBack;
		}

		private void LogOptions()
		{
			_logger.PostEntry("runner options");
			_logger.PostEntryNoTimestamp("server . . . . . . :  {0}", _settings.Server);
			_logger.PostEntryNoTimestamp("project folder . . :  {0}", _settings.ScriptProjectFolder);
			_logger.PostEntryNoTimestamp("log folder . . . . :  {0}", _settings.FileLoggerSettings.BasePath);
			_logger.PostEntryNoTimestamp("unified script path:  {0}", _settings.UnifiedScriptPath);
			_logger.PostEntryNoTimestamp("use transaction  . :  {0}", _settings.UseTransaction);
			_logger.PostEntryNoTimestamp("rollback trans . . :  {0}", _settings.ShouldRollback);
			_logger.PostEntryNoTimestamp("check scripts only :  {0}", _settings.CheckFilesOnly);
			_logger.PostEntryNoTimestamp("bypass check . . . :  {0}", _settings.BypassCheck);
			_logger.PostBlank();
		}

		private static IScriptParserConsumer CreateParser()
		{
			var parser = new ScriptParser();

			parser.AddRule(ScriptConstants.Blocks.Catalog, txt => new CatalogBlock(txt));
			parser.AddRule(ScriptConstants.Blocks.Options, txt => new OptionsBlock(txt));
			parser.AddRule(ScriptConstants.Blocks.Need, txt => new NeedBlock(txt));
			parser.AddRule(ScriptConstants.Blocks.Ignore, txt => new IgnoreScriptBlock(txt));
			parser.AddRule(ScriptConstants.Blocks.Pre, txt => new SqlCommandBlock(ScriptConstants.Blocks.Pre, txt, SqlScriptPhase.Pre));
			parser.AddRule(ScriptConstants.Blocks.Main, txt => new SqlCommandBlock(ScriptConstants.Blocks.Main, txt, SqlScriptPhase.Main));
			parser.AddRule(ScriptConstants.Blocks.Post, txt => new SqlCommandBlock(ScriptConstants.Blocks.Post, txt, SqlScriptPhase.Post));

			parser.SetFallbackRule((kw, txt) => new UnexpectedBlock(kw, txt));

			return parser;
		}

		private IScriptCheckerConsumer CreateChecker()
		{
			var checker = new ScriptChecker();

			foreach (var pattern in _config.ValidationFilterConfig.WarnOn)
				checker.AddValidationPattern(pattern, ScriptCheck.Warning);

			foreach (var pattern in _config.ValidationFilterConfig.HaltOn)
				checker.AddValidationPattern(pattern, ScriptCheck.Failed);

			return checker;
		}

		private Project LoadProject(IScriptParserConsumer parser, IScriptCheckerConsumer checker)
		{
			var loader = new ProjectLoader(_settings.ScriptProjectFolder, parser, checker, _logger);
			if (loader.Load() == Returned.Failure)
				throw new RunnerException("Issues found loading project.  Review log for error information.");

			return new Project(loader.Configuration, loader.AllScripts);
		}

		private void DeployScripts(IRunnerSettings settings, IScriptDeployerConfig config, IProject project, ISqlConnectionManager connectionManager)
		{
			using (var deployer = GetDeployer(settings, config, project, connectionManager))
			{
				if (deployer.CanProceed() == false)
					throw new RunnerException("Cannot proceed! A more recent version has already been deployed.", RunnerResolution.StaleDeployment);

				if (deployer.DeployScripts() == Returned.Failure)
					throw new RunnerException("Deploy scripts failed.");
			}
		}

		private IScriptDeployer GetDeployer(IRunnerSettings settings, IScriptDeployerConfig config, IProject project, ISqlConnectionManager connectionManager)
		{
			return settings.HasUnifiedScriptPath 
				? (IScriptDeployer)new ScriptFileDeployer(settings.UnifiedScriptPath, config, project, _logger) 
				: new SqlServerDeployer(config, project, connectionManager, _logger);
		}

		private readonly ILoggerSync _logger;
		private readonly IScriptDeployerConfig _config;
		private readonly IRunnerSettings _settings;
	}
}