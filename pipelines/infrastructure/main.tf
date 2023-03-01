terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "3.41.0"
    }
  }
  required_version = "1.3.9"

  backend "azurerm" {
    resource_group_name  = var.BACKEND_AZURERM_RESOURCEGROUP_NAME
    storage_account_name = var.BACKEND_AZURERM_STORAGE_ACCOUNT_NAME
    container_name       = var.BACKEND_AZURERM_CONTAINER_NAME
    key                  = var.BACKEND_AZURERM_KEY
  }
}

provider "azurerm" {
  features {}
}

data "azurerm_client_config" "current" {
}

#
# Resource group
#

resource "azurerm_resource_group" "rg" {
  name     = var.RESOURCEGROUP_NAME
  location = var.RESOURCEGROUP_LOCATION
}

#
# Cosmos
#

resource "azurerm_cosmosdb_account" "dba" {
  name                = "cosmos-pocif-dev-wus2-1"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  geo_location {
    location          = azurerm_resource_group.rg.location
    failover_priority = 0
  }

  consistency_policy {
    consistency_level = "Session"
  }

  capabilities {
    name = "EnableServerless"
  }

  capacity {
    total_throughput_limit = 4000
  }
  depends_on = [
    azurerm_resource_group.rg
  ]
}

resource "azurerm_cosmosdb_sql_database" "db" {
  name                = "pocif"
  resource_group_name = azurerm_cosmosdb_account.dba.resource_group_name
  account_name        = azurerm_cosmosdb_account.dba.name
  depends_on = [
    azurerm_cosmosdb_account.dba
  ]
}

resource "azurerm_cosmosdb_sql_container" "dbc" {
  name                  = "samples"
  resource_group_name   = azurerm_cosmosdb_sql_database.db.resource_group_name
  account_name          = azurerm_cosmosdb_sql_database.db.account_name
  database_name         = azurerm_cosmosdb_sql_database.db.name
  partition_key_path    = "/id"
  partition_key_version = 2
}

data "azurerm_cosmosdb_sql_role_definition" "sql_contributor_rd" {
  resource_group_name = azurerm_resource_group.rg.name
  account_name        = azurerm_cosmosdb_account.dba.name
  role_definition_id  = "00000000-0000-0000-0000-000000000002"
}

resource "azurerm_cosmosdb_sql_role_assignment" "sql_contributor_ra_group" {
  resource_group_name = azurerm_resource_group.rg.name
  account_name        = azurerm_cosmosdb_account.dba.name
  role_definition_id  = data.azurerm_cosmosdb_sql_role_definition.sql_contributor_rd.id
  principal_id        = var.GROUP_OBJECT_ID
  scope               = azurerm_cosmosdb_account.dba.id
}

resource "azurerm_cosmosdb_sql_role_assignment" "sql_contributor_ra_app" {
  resource_group_name = azurerm_resource_group.rg.name
  account_name        = azurerm_cosmosdb_account.dba.name
  role_definition_id  = data.azurerm_cosmosdb_sql_role_definition.sql_contributor_rd.id
  principal_id        = azurerm_windows_function_app.func.identity.0.principal_id
  scope               = azurerm_cosmosdb_account.dba.id
}

#
# Service Bus
#

resource "azurerm_servicebus_namespace" "sb" {
  name                = "sb-pocif-dev-wus2-1"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "Basic"
}

resource "azurerm_servicebus_queue" "sbq" {
  name         = "sbq-pocif-dev-wus2-1"
  namespace_id = azurerm_servicebus_namespace.sb.id
}

resource "azurerm_role_assignment" "sb_dataowner_group" {
  scope                = azurerm_servicebus_namespace.sb.id
  role_definition_name = "Azure Service Bus Data Owner"
  principal_id         = var.GROUP_OBJECT_ID
}

resource "azurerm_role_assignment" "sb_dataowner_app" {
  scope                = azurerm_servicebus_namespace.sb.id
  role_definition_name = "Azure Service Bus Data Owner"
  principal_id         = azurerm_windows_function_app.func.identity.0.principal_id
}

#
# Event grid
#

resource "azurerm_eventgrid_topic" "evgt" {
  name                = "evgt-pocif-dev-wus2-1"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
}

resource "azurerm_eventgrid_event_subscription" "evgs" {
  name  = "evgs-pocif-dev-wus2-1"
  scope = azurerm_eventgrid_topic.evgt.id
  depends_on = [
    azurerm_eventgrid_topic.evgt,
    azurerm_function_app_function.PocEventGridTriggerAsync
  ]

  azure_function_endpoint {
    function_id                       = "${azurerm_windows_function_app.func.id}/functions/PocEventGridTriggerAsync"
    max_events_per_batch              = 1
    preferred_batch_size_in_kilobytes = 64
  }
}

resource "azurerm_eventgrid_system_topic" "egst" {
  name                   = "egst-pocif-dev-wus2-1"
  resource_group_name    = azurerm_resource_group.rg.name
  location               = azurerm_resource_group.rg.location
  source_arm_resource_id = azurerm_app_configuration.appconf.id
  topic_type             = "Microsoft.AppConfiguration.ConfigurationStores"
}

resource "azurerm_eventgrid_system_topic_event_subscription" "evgsappcs" {
  name                                 = "evgsappcs-pocif-dev-wus2-1"
  system_topic                         = azurerm_eventgrid_system_topic.egst.name
  resource_group_name                  = azurerm_resource_group.rg.name
  advanced_filtering_on_arrays_enabled = true
  included_event_types = [
    "Microsoft.AppConfiguration.KeyValueModified",
    "Microsoft.AppConfiguration.KeyValueDeleted"
  ]
  depends_on = [
    azurerm_app_configuration.appconf,
    azurerm_function_app_function.PocAppConfigTriggerAsync
  ]

  azure_function_endpoint {
    function_id                       = "${azurerm_windows_function_app.func.id}/functions/PocAppConfigTriggerAsync"
    max_events_per_batch              = 1
    preferred_batch_size_in_kilobytes = 64
  }
  advanced_filter {
    string_contains {
      key    = "Subject"
      values = ["Pocif:Sentinel"]
    }
  }
}

resource "azurerm_role_assignment" "evgt_datasender_group" {
  scope                = azurerm_eventgrid_topic.evgt.id
  role_definition_name = "EventGrid Data Sender"
  principal_id         = var.GROUP_OBJECT_ID
  depends_on = [
    azurerm_app_configuration.appconf
  ]
}

resource "azurerm_role_assignment" "evgt_datasender_app" {
  scope                = azurerm_eventgrid_topic.evgt.id
  role_definition_name = "EventGrid Data Sender"
  principal_id         = azurerm_windows_function_app.func.identity.0.principal_id
}

#
# App Configuration
#

resource "azurerm_app_configuration" "appconf" {
  name                = "appcs-pocif-dev-wus2-1"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
}

resource "azurerm_role_assignment" "appconf_dataowner" {
  scope                = azurerm_app_configuration.appconf.id
  role_definition_name = "App Configuration Data Owner"
  principal_id         = data.azurerm_client_config.current.object_id
}

resource "azurerm_role_assignment" "appconf_datareader_group" {
  scope                = azurerm_app_configuration.appconf.id
  role_definition_name = "App Configuration Data Reader"
  principal_id         = var.GROUP_OBJECT_ID
}

resource "azurerm_role_assignment" "appconf_datareader_app" {
  scope                = azurerm_app_configuration.appconf.id
  role_definition_name = "App Configuration Data Reader"
  principal_id         = azurerm_windows_function_app.func.identity.0.principal_id
}

resource "azurerm_app_configuration_key" "cosmos_container_id" {
  configuration_store_id = azurerm_app_configuration.appconf.id
  key                    = "Pocif:CosmosContainerId"
  value                  = azurerm_cosmosdb_sql_container.dbc.name
  depends_on = [
    azurerm_role_assignment.appconf_dataowner
  ]
}

resource "azurerm_app_configuration_key" "cosmos_database_id" {
  configuration_store_id = azurerm_app_configuration.appconf.id
  key                    = "Pocif:CosmosDatabaseId"
  value                  = azurerm_cosmosdb_sql_database.db.name
  depends_on = [
    azurerm_role_assignment.appconf_dataowner
  ]
}

resource "azurerm_app_configuration_key" "cosmos_endpoint" {
  configuration_store_id = azurerm_app_configuration.appconf.id
  key                    = "Pocif:CosmosEndpoint"
  value                  = azurerm_cosmosdb_account.dba.endpoint
  depends_on = [
    azurerm_role_assignment.appconf_dataowner
  ]
}

resource "azurerm_app_configuration_key" "event_grid_endpoint" {
  configuration_store_id = azurerm_app_configuration.appconf.id
  key                    = "Pocif:EventGridEndpoint"
  value                  = azurerm_eventgrid_topic.evgt.endpoint
  depends_on = [
    azurerm_role_assignment.appconf_dataowner
  ]
}

resource "azurerm_app_configuration_key" "message_delay_in_seconds" {
  configuration_store_id = azurerm_app_configuration.appconf.id
  key                    = "Pocif:MessageDelayInSeconds"
  value                  = "5"
  depends_on = [
    azurerm_role_assignment.appconf_dataowner
  ]
}

resource "azurerm_app_configuration_key" "sentinel" {
  configuration_store_id = azurerm_app_configuration.appconf.id
  key                    = "Pocif:Sentinel"
  value                  = "1"
  depends_on = [
    azurerm_role_assignment.appconf_dataowner
  ]
}

resource "azurerm_app_configuration_key" "service_bus_endpoint" {
  configuration_store_id = azurerm_app_configuration.appconf.id
  key                    = "Pocif:ServiceBusEndpoint"
  value                  = "${azurerm_servicebus_namespace.sb.name}.servicebus.windows.net"
  depends_on = [
    azurerm_role_assignment.appconf_dataowner
  ]
}

resource "azurerm_app_configuration_key" "service_bus_queue_name" {
  configuration_store_id = azurerm_app_configuration.appconf.id
  key                    = "Pocif:ServiceBusQueueName"
  value                  = azurerm_servicebus_queue.sbq.name
  depends_on = [
    azurerm_role_assignment.appconf_dataowner
  ]
}

#
# FunctionApp
#

resource "azurerm_storage_account" "st" {
  name                     = "stpocifdevwus23"
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = azurerm_resource_group.rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "Storage"
}

resource "azurerm_service_plan" "asp" {
  name                = "asp-pocif-dev-wus2-1"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  os_type             = "Windows"
  sku_name            = "Y1"
}

resource "azurerm_application_insights" "appi" {
  name                = "appi-pocif-dev-wus2-1"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  application_type    = "web"
}

resource "azurerm_windows_function_app" "func" {
  name                       = "func-pocif-dev-wus2-1"
  resource_group_name        = azurerm_resource_group.rg.name
  location                   = azurerm_resource_group.rg.location
  storage_account_name       = azurerm_storage_account.st.name
  storage_account_access_key = azurerm_storage_account.st.primary_access_key
  service_plan_id            = azurerm_service_plan.asp.id
  https_only                 = true
  builtin_logging_enabled    = false
  depends_on = [
    azurerm_cosmosdb_sql_container.dbc,
    azurerm_servicebus_queue.sbq,
    azurerm_eventgrid_topic.evgt,
    azurerm_eventgrid_system_topic.egst,
    azurerm_app_configuration.appconf
  ]
  app_settings = {
    AzureWebJobsServiceBus__fullyQualifiedNamespace = "${azurerm_servicebus_namespace.sb.name}.servicebus.windows.net"
    AppConfigurationEndpoint                        = azurerm_app_configuration.appconf.endpoint
    ServiceBusQueueName                             = azurerm_servicebus_queue.sbq.name
    WEBSITE_RUN_FROM_PACKAGE                        = 0
    WEBSITE_ENABLE_SYNC_UPDATE_SITE                 = true
  }
  identity {
    type = "SystemAssigned"
  }
  site_config {
    application_insights_connection_string = azurerm_application_insights.appi.connection_string
    application_stack {
      use_dotnet_isolated_runtime = true
      dotnet_version              = "v7.0"
    }
  }
  lifecycle {
    ignore_changes = [
      app_settings["WEBSITE_RUN_FROM_PACKAGE"],
      tags
    ]
  }
}

resource "azurerm_function_app_function" "PocEventGridTriggerAsync" {
  name            = "PocEventGridTriggerAsync"
  function_app_id = azurerm_windows_function_app.func.id
  language        = "CSharp"
  config_json = jsonencode({
    "bindings" = [
      {
        "direction" = "In"
        "name"      = "eventGridEvent"
        "type"      = "eventGridTrigger"
      },
    ]
  })

  lifecycle {
    ignore_changes = [
      config_json
    ]
  }
}

resource "azurerm_function_app_function" "PocAppConfigTriggerAsync" {
  name            = "PocAppConfigTriggerAsync"
  function_app_id = azurerm_windows_function_app.func.id
  language        = "CSharp"
  config_json = jsonencode({
    "bindings" = [
      {
        "direction" = "In"
        "name"      = "eventGridEvent"
        "type"      = "eventGridTrigger"
      },
    ]
  })

  lifecycle {
    ignore_changes = [
      config_json
    ]
  }
}
