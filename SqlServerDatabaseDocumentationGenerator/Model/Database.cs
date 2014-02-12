﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace net.datacowboy.SqlServerDatabaseDocumentationGenerator.Model
{
	public class Database : IDbObject
	{

		public string DatabaseName { get; set; }

		public int ObjectId { get; set; }

		public IList<Schema> Schemas { get; set; }

	}
}