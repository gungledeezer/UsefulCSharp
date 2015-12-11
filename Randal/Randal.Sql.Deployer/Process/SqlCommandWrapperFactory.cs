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

using System.Data.SqlClient;

namespace Randal.Sql.Deployer.Process
{
	public interface ISqlCommandWrapperFactory
	{
		ISqlCommandWrapper CreateCommand(SqlConnection connection, SqlTransaction transaction, string commandText,
			params object[] values);
	}

	public sealed class SqlCommandWrapperFactory : ISqlCommandWrapperFactory
	{
		public ISqlCommandWrapper CreateCommand(SqlConnection connection, SqlTransaction transaction, string commandText,
			params object[] values)
		{
			return new SqlCommandWrapper(connection, transaction, commandText, values);
		}
	}
}