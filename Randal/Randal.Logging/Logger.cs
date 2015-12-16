﻿// Useful C#
// Copyright (C) 2014-2016 Nicholas Randal
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Randal.Logging
{
	public interface ILogger
	{
		Task PostEntryAsync(string message, params object[] values);

		Task PostEntryAsync(Verbosity verbosity, string message, params object[] values);

		Task PostBlankAsync(Verbosity verbosity = Verbosity.Info);

		Task PostEntryNoTimestampAsync(string message, params object[] values);

		Task PostEntryNoTimestampAsync(Verbosity verbosity, string message, params object[] values);

		Task PostExceptionAsync(Exception ex);

		Task PostExceptionAsync(Exception ex, string message, params object[] values);
	}

	public sealed class Logger : ILogger, IDisposable
	{
		public Logger(int maxBufferCapacity = 100)
		{
			_cancellationSource = new CancellationTokenSource();

			_buffer = new BufferBlock<ILogEntry>(
				new DataflowBlockOptions
				{
					BoundedCapacity = maxBufferCapacity,
					CancellationToken = _cancellationSource.Token
				});

			_broadcast = new BroadcastBlock<ILogEntry>(entry => entry, new DataflowBlockOptions { CancellationToken = _cancellationSource.Token });

			_buffer.LinkTo(_broadcast, new DataflowLinkOptions { PropagateCompletion = true });

			_sinks = new List<ActionBlock<ILogEntry>>();
		}

		public void AddLogSink(ILogSink logSink)
		{
			var block = new ActionBlock<ILogEntry>(entry => logSink.Post(entry), new ExecutionDataflowBlockOptions { CancellationToken = _cancellationSource.Token });
			_sinks.Add(block);
			_broadcast.LinkTo(block, new DataflowLinkOptions { PropagateCompletion = true });
		}

		public async Task CompleteAsync()
		{
			try
			{
				await Task.WhenAll(_sinks.Select(x => x.Completion).ToArray());
			}
			catch (OperationCanceledException) { }
		}

		public async Task PostEntryAsync(string message, params object[] values)
		{
			await PostEntryAsync(Verbosity.Info, message, values);
		}

		public async Task PostEntryAsync(Verbosity verbosity, string message, params object[] values)
		{
			var formatted = string.Format(message, values);

			await _buffer.SendAsync(new LogEntry(formatted, verbosity));
		}

		public async Task PostBlankAsync(Verbosity verbosity = Verbosity.Info)
		{
			await _buffer.SendAsync(new LogEntryNoTimestamp(string.Empty, verbosity));
		}

		public async Task PostEntryNoTimestampAsync(string message, params object[] values)
		{
			await PostEntryNoTimestampAsync(Verbosity.Info, message, values);
		}

		public async Task PostEntryNoTimestampAsync(Verbosity verbosity, string message, params object[] values)
		{
			var formatted = string.Format(message, values);

			await _buffer.SendAsync(new LogEntryNoTimestamp(formatted, verbosity));
		}

		public async Task PostExceptionAsync(Exception ex)
		{
			await _buffer.SendAsync(new LogExceptionEntry(ex));
		}

		public async Task PostExceptionAsync(Exception ex, string message, params object[] values)
		{
			var formatted = string.Format(message, values);

			await _buffer.SendAsync(new LogExceptionEntry(ex, formatted));
		}

		public void Dispose()
		{
			_buffer.Complete();

			if (_cancellationSource.IsCancellationRequested)
				_cancellationSource.Cancel();

			_cancellationSource.Dispose();
		}

		private CancellationTokenSource _cancellationSource;
		private BufferBlock<ILogEntry> _buffer;
		private BroadcastBlock<ILogEntry> _broadcast;
		private List<ActionBlock<ILogEntry>> _sinks;
	}
}
