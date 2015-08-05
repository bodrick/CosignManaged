// CosignModule.cs
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

namespace CosignManagedAdminClient
{
    using Microsoft.Web.Management.Client;
    using Microsoft.Web.Management.Server;
    using System;
    using System.Drawing;
    using System.IO;

    internal class CosignModule : Module
    {
        protected override void Initialize(IServiceProvider serviceProvider, ModuleInfo moduleInfo)
        {
            base.Initialize(serviceProvider, moduleInfo);

            // register the Module Page - RequestPage
            var controlPanel = (IControlPanel)GetService(typeof(IControlPanel));
            Stream icoStream = GetType().Assembly.GetManifestResourceStream("CosignManagedAdminClient.cosign_3d_lg.gif");
            if (icoStream != null)
            {
                var ico = new Bitmap(icoStream);
                icoStream.Close();
                var modulePageInfo = new ModulePageInfo(this, typeof(CosignModuleConfigurationPage), "Cosign", "Configure the cosign authentication service", ico, ico);
                controlPanel.RegisterPage(modulePageInfo);
            }
            else
            {
                var modulePageInfo = new ModulePageInfo(this, typeof(CosignModuleConfigurationPage), "Cosign", "Configure the cosign authentication service");
                controlPanel.RegisterPage(modulePageInfo);
            }
        }
    }
}