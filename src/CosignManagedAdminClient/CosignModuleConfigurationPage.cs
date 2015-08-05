// CosignModuleConfigurationPage.cs
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
    using Microsoft.Web.Management.Client.Win32;
    using Microsoft.Web.Management.Server;
    using System;

    internal class CosignModuleConfigurationPage : ModulePropertiesPage
    {
        private CosignModuleServiceProxy _serviceProxy;
        private PropertyBag _bag;
        private PropertyBag _clone;

        protected override bool CanApplyChanges
        {
            get { return HasChanges; }
        }

        protected override bool CanRefresh
        {
            get { return true; }
        }

        private CosignModuleServiceProxy ServiceProxy
        {
            get
            {
                return _serviceProxy
                       ?? (_serviceProxy = (CosignModuleServiceProxy)CreateProxy(typeof(CosignModuleServiceProxy)));
            }
        }

        protected override PropertyBag GetProperties()
        {
            return ServiceProxy.GetAppSettings();
        }

        protected override void ProcessProperties(PropertyBag properties)
        {
            _bag = properties;
            _clone = _bag.Clone();
            var connection = (Connection)GetService(typeof(Connection));

            var info = (CosignModuleCustomPropertiesInfo)TargetObject;
            if (info == null)
            {
                info = new CosignModuleCustomPropertiesInfo(this, _clone, connection.ConfigurationPath.PathType, connection.IsLocalConnection);
                TargetObject = info;
            }
            else
            {
                info.Initialize(_clone, connection.ConfigurationPath.PathType, connection.IsLocalConnection);
            }
            ClearDirty();
        }

        protected override PropertyBag UpdateProperties(out bool updateSuccessful)
        {
            updateSuccessful = false;
            try
            {
                ServiceProxy.UpdateAppSettings(_clone);
                _bag = _clone;
                updateSuccessful = true;
            }
            catch (Exception ex)
            {
                OnException(ex);
            }
            return _bag;
        }

        protected override void OnException(Exception ex)
        {
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }
            ShowError(ex, false);
        }
    }
}