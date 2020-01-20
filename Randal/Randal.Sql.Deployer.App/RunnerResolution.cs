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

namespace Randal.Sql.Deployer.App
{
	public enum RunnerResolution
	{
		Committed		= -1,
		ValidationOnly	= -2,
		RolledBack		= -3,
		StaleDeployment = -50,
		ExceptionThrown = -999
	}
}