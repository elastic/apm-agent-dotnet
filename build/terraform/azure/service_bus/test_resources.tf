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

# configuration is sourced from the following environment variables:
# ARM_CLIENT_ID
# ARM_CLIENT_SECRET
# ARM_SUBSCRIPTION_ID
# ARM_TENANT_ID
# 
# See https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/guides/service_principal_client_secret
# for creating a Service Principal and Client Secret
data "azurerm_client_config" "current" {
}

variable "resource_group" {
	type = string
	description = "The name of the resource group to create"

	# TODO validation
}

variable "location" {
	type = string
	description = "The Azure location in which to deploy resources"
	
	# TODO validation
}

variable "servicebus_namespace" {
	type = string
	description = "The name of the servicebus namespace to create"
	
//	validation {
//		condition = can(regex("^[a-zA-Z][a-zA-Z0-9-]{5,49}$", var.servicebus_namespace)) && can(regex("[^\\-|\\-sb|\\-mgmt]$", var.servicebus_namespace))
//		error_message = "The value must be a valid service bus namespace. See https://docs.microsoft.com/en-us/rest/api/servicebus/create-namespace."
//	}
}


resource "azurerm_resource_group" "servicebus_resource_group" {
	name     = var.resource_group
	location = var.location
}

resource "azurerm_servicebus_namespace" "servicebus_namespace" {
	location = azurerm_resource_group.servicebus_resource_group.location
	name = var.servicebus_namespace
	resource_group_name = azurerm_resource_group.servicebus_resource_group.name
	sku = "Basic"
	depends_on = [azurerm_resource_group.servicebus_resource_group]
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
	scope = azurerm_resource_group.servicebus_resource_group.id
	depends_on = [azurerm_servicebus_namespace.servicebus_namespace]
}

# random name to generate for the contributor role assignment
resource "random_uuid" "data_owner_role" {
	keepers = {
		client_id = data.azurerm_client_config.current.client_id
	}
}

resource "azurerm_role_assignment" "servicebus_data_owner_role" {
	name = random_uuid.data_owner_role.result
	principal_id = data.azurerm_client_config.current.object_id
	role_definition_name = "Azure Service Bus Data Owner"
	scope = azurerm_resource_group.servicebus_resource_group.id
	depends_on = [azurerm_servicebus_namespace.servicebus_namespace]
}

# following role assignment, there can be a delay of up to ~1 minute
# for the assignments to propagate in Azure. You may need to introduce
# a wait before using the Azure resources created.

output "connection_string" {
	value = azurerm_servicebus_namespace.servicebus_namespace.default_primary_connection_string
	description = "The service bus primary connection string"
	sensitive = true
}

