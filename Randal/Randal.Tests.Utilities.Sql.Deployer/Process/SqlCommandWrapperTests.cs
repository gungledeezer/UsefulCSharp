﻿/*
Useful C#
Copyright (C) 2014  Nicholas Randal

Useful C# is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
*/

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Randal.Core.Testing.UnitTest;
using System.Data.SqlClient;
using Randal.Utilities.Sql.Deployer.Process;

namespace Randal.Tests.Utilities.Sql.Deployer.Process
{
	[TestClass]
	public sealed class SqlCommandWrapperTests : BaseUnitTest<SqlCommandWrapperThens>
	{
		[TestInitialize]
		public override void Setup()
		{
			base.Setup();
		}

		[TestCleanup]
		public void Teardown()
		{
			Then.Wrapper.Dispose();
		}

		[TestMethod]
		public void ShouldHaveWrapperWhenCreating()
		{
			Given.Database = "master";
			When(Creating);
			Then.Wrapper.Should().NotBeNull().And.BeAssignableTo<ISqlCommandWrapper>();
		}

		[TestMethod, ExpectedException(typeof (InvalidOperationException))]
		public void ShouldThrowSqlExceptionWhenExecutingGivenNoSqlServer()
		{
			Given.Database = "master";
			When(Creating, ExecutingCommand);
		}

		private void ExecutingCommand()
		{
			
			Then.Wrapper.Execute(Given.Database);
		}

		private void Creating()
		{
			Then.Wrapper = new SqlCommandWrapper(new SqlConnection(), string.Empty);
		}
	}

	public sealed class SqlCommandWrapperThens
	{
		public SqlCommandWrapper Wrapper;
	}
}
