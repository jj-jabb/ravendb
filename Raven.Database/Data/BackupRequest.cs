//-----------------------------------------------------------------------
// <copyright file="BackupRequest.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using Raven.Abstractions.Data;

namespace Raven.Database.Data
{
	public class BackupRequest
	{
		public string BackupLocation { get; set; }
		public DatabaseDocument DatabaseDocument { get; set; }
	}
}
