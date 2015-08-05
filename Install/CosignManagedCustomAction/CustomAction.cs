// CustomAction.cs
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

using System.Reflection;

namespace CosignManagedCustomAction
{
    using Microsoft.Deployment.WindowsInstaller;
    using Microsoft.Web.Administration;
    using Microsoft.Win32;
    using System;
    using System.Security.AccessControl;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Principal;

    public class CustomActions
    {
        [CustomAction]
        public static ActionResult Install(Session session)
        {
            //System.Diagnostics.Debugger.Launch();
            session.Log("Begin Install");
            const string defaultUser = "BUILTIN\\IIS_IUSRS";
            //var clientAssemblyName=new AssemblyName(session.CustomActionData["AdminClientAssembly"]);
            var serverAssemblyName = new AssemblyName(session.CustomActionData["AdminServerAssembly"]);
            var cosignAssemblyName = new AssemblyName(session.CustomActionData["CosignAssembly"]);
            
            // Add permissions to the SystemCertificates registry key (not sure if needed)
            RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\SystemCertificates\MY", true);
            if (rk != null)
            {
                RegistrySecurity rs = rk.GetAccessControl();
                rs.AddAccessRule(new RegistryAccessRule(defaultUser, RegistryRights.ReadKey, InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
                rk.SetAccessControl(rs);
                rk.Close();
            }

            // Add permissions to all valid certificates in the store
            var certificateStore = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            certificateStore.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certificateCollection = certificateStore.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, true);
            foreach (X509Certificate2 x509 in certificateCollection)
            {
                try
                {
                    var rsa = x509.PrivateKey as RSACryptoServiceProvider;
                    if (rsa != null)
                    {
                        var cspParams = new CspParameters(rsa.CspKeyContainerInfo.ProviderType, rsa.CspKeyContainerInfo.ProviderName, rsa.CspKeyContainerInfo.KeyContainerName)
                        {
                            Flags =
                                CspProviderFlags.UseExistingKey | CspProviderFlags.UseMachineKeyStore,
                            CryptoKeySecurity = rsa.CspKeyContainerInfo.CryptoKeySecurity
                        };

                        var account = new NTAccount(defaultUser);
                        cspParams.CryptoKeySecurity.AddAccessRule(new CryptoKeyAccessRule(account, CryptoKeyRights.GenericRead, AccessControlType.Allow));
                        using (var rsa2 = new RSACryptoServiceProvider(cspParams))
                        {
                            // Only created to persist the rule change in the CryptoKeySecurity
                        }
                    }
                }
                catch (Exception)
                {
                    session.Log("Invalid Certificate");                    
                }                
            }

            using (var serverManager = new ServerManager())
            {
                // Add cosign admin module to IIS administrator for server configuration
                Configuration adminConfig = serverManager.GetAdministrationConfiguration();
                ConfigurationSection moduleProviderSection = adminConfig.GetSection("moduleProviders");
                ConfigurationElementCollection moduleProvidersCollection = moduleProviderSection.GetCollection();

                ConfigurationElement oldModuleProviderElement = null;
                foreach (ConfigurationElement moduleProviderElement in moduleProvidersCollection)
                {
                    if (moduleProviderElement.Attributes["name"].Value.ToString() == "Cosign")
                    {
                        oldModuleProviderElement = moduleProviderElement;
                    }
                }
                if (oldModuleProviderElement != null)
                {
                    moduleProvidersCollection.Remove(oldModuleProviderElement);
                }

                ConfigurationElement cosignAdminModuleProvider = moduleProvidersCollection.CreateElement("add");
                cosignAdminModuleProvider.Attributes["name"].Value = "Cosign";
                cosignAdminModuleProvider.Attributes["type"].Value = String.Format("CosignManagedAdminServer.CosignModuleProvider, CosignManagedAdminServer, Version={0}, Culture=neutral, PublicKeyToken={1}", serverAssemblyName.Version, BitConverter.ToString(serverAssemblyName.GetPublicKeyToken()).Replace("-", ""));
                moduleProvidersCollection.Add(cosignAdminModuleProvider);

                // Add cosign admin module to IIS administrator for site/application configuration
                ConfigurationElement oldAdminModuleElement = null;
                ConfigurationSection adminModulesSection = adminConfig.GetSection("modules");
                ConfigurationElementCollection adminModulesCollection = adminModulesSection.GetCollection();

                foreach (ConfigurationElement moduleProviderElement in adminModulesCollection)
                {
                    if (moduleProviderElement.Attributes["name"].Value.ToString() == "Cosign")
                    {
                        oldAdminModuleElement = moduleProviderElement;
                    }
                }
                if (oldAdminModuleElement != null)
                {
                    adminModulesCollection.Remove(oldAdminModuleElement);
                }

                ConfigurationElement cosignAdminModule = adminModulesCollection.CreateElement("add");
                cosignAdminModule.Attributes["name"].Value = "Cosign";
                adminModulesCollection.Add(cosignAdminModule);

                // Add configSection to sectionGroup
                Configuration appHostConfig = serverManager.GetApplicationHostConfiguration();
                SectionGroup webServerSectionGroup = appHostConfig.RootSectionGroup.SectionGroups["system.webServer"];
                SectionDefinition cosignSectionDef = webServerSectionGroup.Sections["cosign"];
                if (cosignSectionDef == null)
                {
                    cosignSectionDef = webServerSectionGroup.Sections.Add("cosign");
                    cosignSectionDef.OverrideModeDefault = "Allow";
                }
                else
                {
                    cosignSectionDef.OverrideModeDefault = "Allow";
                }

                // Add handler for cosign module to server configuration
                ConfigurationElement oldHandlerElement = null;
                ConfigurationSection serverHandlersSection = appHostConfig.GetSection("system.webServer/handlers");
                ConfigurationElementCollection serverHandlersCollection = serverHandlersSection.GetCollection();
                foreach (ConfigurationElement moduleProviderElement in serverHandlersCollection)
                {
                    if (moduleProviderElement.Attributes["name"].Value.ToString() == "Cosign")
                    {
                        oldHandlerElement = moduleProviderElement;
                    }
                }
                if (oldHandlerElement != null)
                {
                    serverHandlersCollection.Remove(oldHandlerElement);
                }

                ConfigurationElement cosignHandlerElement = serverHandlersCollection.CreateElement("add");
                cosignHandlerElement["name"] = @"Cosign";
                cosignHandlerElement["path"] = @"/cosign/valid*";
                cosignHandlerElement["verb"] = @"*";
                cosignHandlerElement["type"] = String.Format("CosignManaged.CosignHandler, CosignManaged, Version={0}, Culture=neutral, PublicKeyToken={1}", cosignAssemblyName.Version, BitConverter.ToString(cosignAssemblyName.GetPublicKeyToken()).Replace("-", ""));
                cosignHandlerElement["preCondition"] = @"integratedMode";
                cosignHandlerElement["resourceType"] = @"Unspecified";
                serverHandlersCollection.AddAt(0, cosignHandlerElement);

                // Add cosign module to server configuration
                ConfigurationElement oldModuleElement = null;
                ConfigurationSection serverModulesSection = appHostConfig.GetSection("system.webServer/modules");
                ConfigurationElementCollection serverModulesCollection = serverModulesSection.GetCollection();
                foreach (ConfigurationElement moduleProviderElement in serverModulesCollection)
                {
                    if (moduleProviderElement.Attributes["name"].Value.ToString() == "Cosign")
                    {
                        oldModuleElement = moduleProviderElement;
                    }
                }
                if (oldModuleElement != null)
                {
                    serverModulesCollection.Remove(oldModuleElement);
                }

                ConfigurationElement cosignModuleElement = serverModulesCollection.CreateElement("add");
                cosignModuleElement["name"] = @"Cosign";
                cosignModuleElement["type"] = String.Format("CosignManaged.CosignModule, CosignManaged, Version={0}, Culture=neutral, PublicKeyToken={1}", cosignAssemblyName.Version, BitConverter.ToString(cosignAssemblyName.GetPublicKeyToken()).Replace("-", ""));
                serverModulesCollection.AddAt(0, cosignModuleElement);

                serverManager.CommitChanges();
            }

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult Uninstall(Session session)
        {
            using (var serverManager = new ServerManager())
            {
                // Remove cosign admin module from IIS administrator for server configuration
                Configuration adminConfig = serverManager.GetAdministrationConfiguration();
                ConfigurationSection moduleProviderSection = adminConfig.GetSection("moduleProviders");
                ConfigurationElementCollection moduleProvidersCollection = moduleProviderSection.GetCollection();
                ConfigurationElement oldModuleProviderElement = null;

                foreach (ConfigurationElement moduleProviderElement in moduleProvidersCollection)
                {
                    if (moduleProviderElement.Attributes["name"].Value.ToString() == "Cosign")
                    {
                        oldModuleProviderElement = moduleProviderElement;
                    }
                }

                if (oldModuleProviderElement != null)
                {
                    moduleProvidersCollection.Remove(oldModuleProviderElement);
                }

                // Remove cosign admin module from IIS administrator for site/application configuration
                ConfigurationSection adminModulesSection = adminConfig.GetSection("modules");
                ConfigurationElementCollection adminModulesCollection = adminModulesSection.GetCollection();
                ConfigurationElement oldAdminModuleElement = null;
                foreach (ConfigurationElement moduleProviderElement in adminModulesCollection)
                {
                    if (moduleProviderElement.Attributes["name"].Value.ToString() == "Cosign")
                    {
                        oldAdminModuleElement = moduleProviderElement;
                    }
                }

                if (oldAdminModuleElement != null)
                {
                    adminModulesCollection.Remove(oldAdminModuleElement);
                }

                // Remove cosign handler from server configuration
                Configuration appHostConfig = serverManager.GetApplicationHostConfiguration();
                ConfigurationSection serverHandlersSection = appHostConfig.GetSection("system.webServer/handlers");
                ConfigurationElementCollection serverHandlersCollection = serverHandlersSection.GetCollection();
                ConfigurationElement oldHandlerElement = null;
                foreach (ConfigurationElement moduleProviderElement in serverHandlersCollection)
                {
                    if (moduleProviderElement.Attributes["name"].Value.ToString() == "Cosign")
                    {
                        oldHandlerElement = moduleProviderElement;
                    }
                }
                if (oldHandlerElement != null)
                {
                    serverHandlersCollection.Remove(oldHandlerElement);
                }

                // Remove cosign module from server configuration
                ConfigurationSection serverModulesSection = appHostConfig.GetSection("system.webServer/modules");
                ConfigurationElementCollection serverModulesCollection = serverModulesSection.GetCollection();
                ConfigurationElement oldModuleElement = null;
                foreach (ConfigurationElement moduleProviderElement in serverModulesCollection)
                {
                    if (moduleProviderElement.Attributes["name"].Value.ToString() == "Cosign")
                    {
                        oldModuleElement = moduleProviderElement;
                    }
                }
                if (oldModuleElement != null)
                {
                    serverModulesCollection.Remove(oldModuleElement);
                }

                serverManager.CommitChanges();
            }
            return ActionResult.Success;
        }
    }
}