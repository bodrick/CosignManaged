// GlobalLogger.cs
//
// Copyright (C) 2011 The Pennsylvania State University
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License as
// published by the Free Software Foundation; either version 2 of
// the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR
// PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public
// License along with this program; if not, write to the Free
// Software Foundation, Inc., 59 Temple Place, Suite 330, Boston,
// MA 02111-1307 USA

namespace CosignManaged
{
    using NLog;
    using NLog.Config;
    using System;

    internal class GlobalLogger
    {
        // A Logger dispenser for the current assembly
        public static readonly LogFactory Instance = new LogFactory(new XmlLoggingConfiguration(GetNLogConfigFilePath()));

        // Use a config file located next to our current assembly dll
        // eg, if the running assembly is c:\path\to\MyComponent.dll
        // the config filepath will be c:\path\to\MyComponent.nlog
        //
        // WARNING: This will not be appropriate for assemblies in the GAC
        private static string GetNLogConfigFilePath()
        {
            // Use some configuration option here instead
            return Environment.ExpandEnvironmentVariables(@"%windir%\system32\inetsrv\Nlog.config");
        }
    }
}