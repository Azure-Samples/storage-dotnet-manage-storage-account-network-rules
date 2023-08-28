// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Samples.Common;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;

namespace ManageStorageAccountNetworkRules
{
    /**
     * Azure Storage sample for managing storage account network rules -
     *  - Create a virtual network and subnet with storage service subnet access enabled
     *  - Create a storage account with access allowed only from the subnet
     *  - Create a public IP address
     *  - Create a virtual machine and associate the public IP address
     *  - Update the storage account with access also allowed from the public IP address
     *  - Update the storage account to restrict incoming traffic to HTTPS.
     */
    public class Program
    {
        private static ResourceIdentifier? _resourceGroupId = null;
        public static async Task RunSample(ArmClient client)
        {
            try
            {
                // ============================================================

                // Create a virtual network and a subnet with storage service subnet access enabled
                Utilities.Log("Creating a Virtual network and a subnet with storage service subnet access enabled...");
                
                // Get default subscription
                SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();
                var rgName = Utilities.CreateRandomName("VirtualNetworkRG");
                Utilities.Log($"creating resource group with name : {rgName}");
                var rgLro = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, rgName, new ResourceGroupData(AzureLocation.EastUS));
                var resourceGroup = rgLro.Value;
                _resourceGroupId = resourceGroup.Id;
                Utilities.Log("Created a resource group with name: " + resourceGroup.Data.Name);
                
                // Create a virtual network
                Utilities.Log("Creating a virtual network...");
                var virtualNetworkName = Utilities.CreateRandomName("VirtualNetwork_");
                var virtualNetworkCollection = resourceGroup.GetVirtualNetworks();
                var subnetName = Utilities.CreateRandomName("subnet_");
                var data = new VirtualNetworkData()
                {
                    Location = AzureLocation.EastUS,
                    AddressPrefixes =
                    {
                        new string("10.0.0.0/28"),
                    },
                };
                var virtualNetworkLro = await virtualNetworkCollection.CreateOrUpdateAsync(WaitUntil.Completed, virtualNetworkName,data);
                var virtualNetwork = virtualNetworkLro.Value;
                Utilities.Log("Created a virtual network with name : " + virtualNetwork.Data.Name);
                
                //Create a subnet
                Utilities.Log("Creating a subnet..");
                var subnetData = new SubnetData()
                {
                    ServiceEndpoints =
                    {
                        new ServiceEndpointProperties()
                        {
                            Service = "Microsoft.Storage"
                        }
                    },
                    Name = subnetName,
                    AddressPrefix = "10.0.0.8/29",
                };
                var subnetLRro = await virtualNetwork.GetSubnets().CreateOrUpdateAsync(WaitUntil.Completed, subnetName, subnetData);
                var subnet = subnetLRro.Value;
                Utilities.Log("Created a subnet with name : " + subnet.Data.Name);

                // ============================================================

                // Create a storage account with access to it allowed only from a specific subnet
                var subnetId = $"{virtualNetwork.Data.Id}/subnets/{subnetName}";
                Utilities.Log($"Creating a storage account with access allowed only from the subnet{subnetId}");
                var storageCollection = resourceGroup.GetStorageAccounts();
                var accountName = Utilities.CreateRandomName("saname");
                var sku = new StorageSku(StorageSkuName.StandardGrs);
                var kind = StorageKind.StorageV2;
                var resourceId = new ResourceIdentifier(subnetId);
                var content = new StorageAccountCreateOrUpdateContent(sku, kind, AzureLocation.EastUS)
                {
                    NetworkRuleSet = new StorageAccountNetworkRuleSet(StorageNetworkDefaultAction.Deny)
                    {
                        VirtualNetworkRules =
                        {
                            new StorageAccountVirtualNetworkRule(resourceId),
                        }
                    }
                };
                var storageAccountLro = await storageCollection.CreateOrUpdateAsync(WaitUntil.Completed,accountName,content);
                var storageAccount = storageAccountLro.Value;
                Utilities.Log("Created a storage account with name : " + storageAccount.Data.Name);

                // ============================================================

                // Create a public IP address
                Utilities.Log("Creating a Public IP address...");
                var publicAddressIPCollection = resourceGroup.GetPublicIPAddresses();
                var publicIPAddressName = Utilities.CreateRandomName("pip");
                var publicIPAddressdata = new PublicIPAddressData()
                {
                    Location = AzureLocation.EastUS,
                    Sku = new PublicIPAddressSku()
                    {
                        Name = PublicIPAddressSkuName.Standard,
                    },
                    PublicIPAddressVersion = NetworkIPVersion.IPv4,
                    PublicIPAllocationMethod = NetworkIPAllocationMethod.Static,
                    DnsSettings = new PublicIPAddressDnsSettings()
                    {
                        DomainNameLabel = publicIPAddressName
                    }
                };
                var publicIPAddressLro = await publicAddressIPCollection.CreateOrUpdateAsync(WaitUntil.Completed, publicIPAddressName, publicIPAddressdata);
                var publicIPAddress = publicIPAddressLro.Value;
                Utilities.Log("Creating a Public IP address with name : " + publicIPAddress.Data.Name);

                // ============================================================
                // Create a virtual machine and associate the public IP address
               
                // Create a virtual network
                Utilities.Log("Creating a virtual network2...");
                var virtualNetworkName2 = Utilities.CreateRandomName("VirtualNetwork2_");
                var virtualNetworkCollection2 = resourceGroup.GetVirtualNetworks();
                var data2 = new VirtualNetworkData()
                {
                    Location = AzureLocation.EastUS,
                    AddressPrefixes =
                    {
                        new string("10.1.0.0/28"),
                    },
                };
                var virtualNetworkLro2 = await virtualNetworkCollection2.CreateOrUpdateAsync(WaitUntil.Completed, virtualNetworkName2, data2);
                var virtualNetwork2 = virtualNetworkLro2.Value;
                Utilities.Log("Created a virtual network2 with name : " + virtualNetwork2.Data.Name);

                //Create a subnet
                Utilities.Log("Creating a subnet2...");
                var subnetName2 = Utilities.CreateRandomName("subnet2_");
                var subnetData2 = new SubnetData()
                {
                    ServiceEndpoints =
                    {
                        new ServiceEndpointProperties()
                        {
                            Service = "Microsoft.Storage"
                        }
                    },
                    Name = subnetName2,
                    AddressPrefix = "10.1.0.0/28",
                };
                var subnetLRro2 = await virtualNetwork2.GetSubnets().CreateOrUpdateAsync(WaitUntil.Completed, subnetName2, subnetData2);
                var subnet2 = subnetLRro2.Value;
                Utilities.Log("Created a subnet2 with name : " + subnet2.Data.Name);

                //Create a networkInterface
                Utilities.Log("Created a networkInterface");
                var networkInterfaceData = new NetworkInterfaceData()
                {
                    Location = AzureLocation.EastUS,
                    IPConfigurations =
                    {
                        new NetworkInterfaceIPConfigurationData()
                        {
                            Name = "internal",
                            Primary = true,
                            Subnet = new SubnetData
                            {
                                Name = subnetName2,
                                Id = new ResourceIdentifier($"{virtualNetwork2.Data.Id}/subnets/{subnetName2}")
                            },
                            PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                            PublicIPAddress = publicIPAddress.Data,
                        }
                    }
                };
                var networkInterfaceName = Utilities.CreateRandomName("networkInterface");
                var nic = (await resourceGroup.GetNetworkInterfaces().CreateOrUpdateAsync(WaitUntil.Completed, networkInterfaceName, networkInterfaceData)).Value;
                Utilities.Log("Created a network interface with name : " + nic.Data.Name);

                //Create a VM with the Public IP address
                Utilities.Log("Creating a VM with the Public IP address...");
                var virtualMachineCollection = resourceGroup.GetVirtualMachines();
                var vmName = Utilities.CreateRandomName("vm");
                var adminUsername = Utilities.CreateRandomName("admin");
                var adminPassword = Utilities.CreateRandomName("Password");
                var computerName = Utilities.CreateRandomName("computer");
                var virtualMachinedata = new VirtualMachineData(AzureLocation.EastUS)
                {
                    HardwareProfile = new VirtualMachineHardwareProfile()
                    {
                        VmSize = VirtualMachineSizeType.StandardD4V2
                    },
                    OSProfile = new VirtualMachineOSProfile()
                    {
                        AdminUsername = adminUsername,
                        AdminPassword = adminPassword,
                        ComputerName = computerName,
                    },
                    NetworkProfile = new VirtualMachineNetworkProfile()
                    {
                        NetworkInterfaces =
                        {
                            new VirtualMachineNetworkInterfaceReference()
                            {
                                Id = nic.Id,
                                Primary = true,
                            }
                        }
                    },    
                    StorageProfile = new VirtualMachineStorageProfile()
                    {
                        OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage)
                        {
                            OSType = SupportedOperatingSystemType.Linux,
                            Caching = CachingType.ReadWrite,
                            ManagedDisk = new VirtualMachineManagedDisk()
                            {
                                StorageAccountType = StorageAccountType.StandardLrs
                            }
                        },
                        ImageReference = new ImageReference()
                        {
                            Publisher = "Canonical",
                            Offer = "UbuntuServer",
                            Sku = "16.04-LTS",
                            Version = "latest",
                        }
                    },
                };
                var virtualMachineLro = await virtualMachineCollection.CreateOrUpdateAsync(WaitUntil.Completed, vmName, virtualMachinedata);
                var linuxVM = virtualMachineLro.Value;
                Utilities.Log($"Created the VM with Id : " + linuxVM.Data.Id);

                // ============================================================

                // Update the storage account so that it can also be accessed from the PublicIP address
                Utilities.Log($"Updating storage account with access also allowed from publicIP : {publicIPAddress.Data.IPAddress}");
                var patch = new StorageAccountPatch()
                {
                    PublicNetworkAccess = StoragePublicNetworkAccess.Enabled,
                    NetworkRuleSet = new StorageAccountNetworkRuleSet(StorageNetworkDefaultAction.Allow)
                    {
                        IPRules =
                        {
                            new StorageAccountIPRule(publicIPAddress.Data.IPAddress)
                            {
                                IPAddressOrRange = publicIPAddress.Data.IPAddress,
                                Action = StorageAccountNetworkRuleAction.Allow
                            }
                        }
                    }
                };
                _ =await storageAccount.UpdateAsync(patch);
                Utilities.Log("Updated storage account");
                
                // ============================================================

                //  Update the storage account to restrict incoming traffic to HTTPS
                Utilities.Log($"Updating storage account with access also allowed from publicIP : {publicIPAddress.Data.IPAddress}");
                var patch2 = new StorageAccountPatch()
                {
                   EnableHttpsTrafficOnly = true,
                };
                _ = await storageAccount.UpdateAsync(patch2);
                Utilities.Log("Updated storage account");
            }
            finally
            {
                try
                {
                    if (_resourceGroupId is not null)
                    {
                        Utilities.Log($"Deleting Resource Group: {_resourceGroupId}");
                        await client.GetResourceGroupResource(_resourceGroupId).DeleteAsync(WaitUntil.Completed);
                        Utilities.Log($"Deleted Resource Group: {_resourceGroupId}");
                    }
                }
                catch (NullReferenceException)
                {
                    Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
                }
                catch (Exception g)
                {
                    Utilities.Log(g);
                }
            }
        }
        
        public static async Task Main(string[] args)
        {
            try
            {
                var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
                var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
                var subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
                ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                ArmClient client = new ArmClient(credential, subscription);
                await RunSample(client);
            }
            catch (Exception e)
            {
                Utilities.Log(e);
            }
        }
    }
}
