﻿// CertificateConverter.cs
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
    using Microsoft.Web.Management.Server;
    using System.ComponentModel;

    internal class CertificateConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var cosignModuleCustomPropertiesInfo = context.Instance as CosignModuleCustomPropertiesInfo;
            if (cosignModuleCustomPropertiesInfo != null)
            {
                PropertyBag bag = cosignModuleCustomPropertiesInfo.Bag;
                object o = bag[CosignGlobals.CertificateCollection];
                if (o != null)
                {
                    var certNames = (string[])o;
                    var cols = new StandardValuesCollection(certNames);
                    return cols;
                }
            }
            return null;
        }
    }
}