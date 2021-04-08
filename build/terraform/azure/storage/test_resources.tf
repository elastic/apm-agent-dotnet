terraform {
	required_providers {
		azurerm = {
			source  = "hashicorp/azurerm"
			version = "=2.46.0"
		}
	}
}

provider "azurerm" {
	features {}
}

data "azurerm_client_config" "current" {
}

resource "random_uuid" "variables" {
}

variable "resource_group" {
	type = string
	description = "The name of the resource group to create"
}

variable "location" {
	type = string
	description = "The Azure location in which to deploy resources"
	default = "westus"
}

variable "storage_account_name" {
	type = string
	description = "The name of the storage account to create"
}


resource "azurerm_resource_group" "storage_resource_group" {
	name     = var.resource_group
	location = var.location
}

resource "azurerm_storage_account" "storage_account" {
  name                     = var.storage_account_name
  resource_group_name      = azurerm_resource_group.storage_resource_group.name
  location                 = azurerm_resource_group.storage_resource_group.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  enable_https_traffic_only = true
}

# random name to generate for the contributor role assignment
resource "random_uuid" "contributor_role" {
	keepers = {
		client_id = data.azurerm_client_config.current.client_id
	}
}

resource "azurerm_role_assignment" "contributor_role" {
	name = random_uuid.contributor_role.result
	principal_id = data.azurerm_client_config.current.object_id
	role_definition_name = "Contributor"
	scope = azurerm_resource_group.storage_resource_group.id
	depends_on = [azurerm_storage_account.storage_account]
}


# following role assignment, there can be a delay of up to ~1 minute
# for the assignments to propagate in Azure. You may need to introduce
# a wait before using the Azure resources created.

output "connection_string" {
	value = azurerm_storage_account.storage_account.primary_connection_string
	description = "The service bus primary connection string"
	sensitive = true
}

