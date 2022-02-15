Feature: Extracting Metadata for Azure App Service

  Background:
    Given an agent configured with
      | setting        | value |
      | cloud_provider | azure |

  Scenario Outline: Azure App Service with all environment variables present in expected format
    Given the following environment variables are present
      | name                   | value                |
      | WEBSITE_OWNER_NAME     | <WEBSITE_OWNER_NAME> |
      | WEBSITE_RESOURCE_GROUP | resource_group       |
      | WEBSITE_SITE_NAME      | site_name            |
      | WEBSITE_INSTANCE_ID    | instance_id          |
    When cloud metadata is collected
    Then cloud metadata is not null
    And cloud metadata 'account.id' is 'f5940f10-2e30-3e4d-a259-63451ba6dae4'
    And cloud metadata 'provider' is 'azure'
    And cloud metadata 'instance.id' is 'instance_id'
    And cloud metadata 'instance.name' is 'site_name'
    And cloud metadata 'project.name' is 'resource_group'
    And cloud metadata 'region' is 'AustraliaEast'
    Examples:
      | WEBSITE_OWNER_NAME                                                                          |
      | f5940f10-2e30-3e4d-a259-63451ba6dae4+elastic-apm-AustraliaEastwebspace                      |
      | f5940f10-2e30-3e4d-a259-63451ba6dae4+appsvc_linux_australiaeast-AustraliaEastwebspace-Linux |

  # WEBSITE_OWNER_NAME is expected to include a + character
  Scenario: WEBSITE_OWNER_NAME environment variable not expected format
    Given the following environment variables are present
    | name                   | value                                                                  |
    | WEBSITE_OWNER_NAME     | f5940f10-2e30-3e4d-a259-63451ba6dae4-elastic-apm-AustraliaEastwebspace |
    | WEBSITE_RESOURCE_GROUP | resource_group                                                         |
    | WEBSITE_SITE_NAME      | site_name                                                              |
    | WEBSITE_INSTANCE_ID    | instance_id                                                            |
    When cloud metadata is collected
    Then cloud metadata is null

  Scenario: Missing WEBSITE_OWNER_NAME environment variable
    Given the following environment variables are present
    | name                   | value                                                                  |
    | WEBSITE_RESOURCE_GROUP | resource_group                                                         |
    | WEBSITE_SITE_NAME      | site_name                                                              |
    | WEBSITE_INSTANCE_ID    | instance_id                                                            |
    When cloud metadata is collected
    Then cloud metadata is null

  Scenario: Missing WEBSITE_RESOURCE_GROUP environment variable
    Given the following environment variables are present
    | name                   | value                                                                  |
    | WEBSITE_OWNER_NAME     | f5940f10-2e30-3e4d-a259-63451ba6dae4+elastic-apm-AustraliaEastwebspace |
    | WEBSITE_SITE_NAME      | site_name                                                              |
    | WEBSITE_INSTANCE_ID    | instance_id                                                            |
    When cloud metadata is collected
    Then cloud metadata is null

  Scenario: Missing WEBSITE_SITE_NAME environment variable
    Given the following environment variables are present
    | name                   | value                                                                  |
    | WEBSITE_OWNER_NAME     | f5940f10-2e30-3e4d-a259-63451ba6dae4+elastic-apm-AustraliaEastwebspace |
    | WEBSITE_RESOURCE_GROUP | resource_group                                                         |
    | WEBSITE_INSTANCE_ID    | instance_id                                                            |
    When cloud metadata is collected
    Then cloud metadata is null

  Scenario: Missing WEBSITE_INSTANCE_ID environment variable
    Given the following environment variables are present
    | name                   | value                                                                  |
    | WEBSITE_OWNER_NAME     | f5940f10-2e30-3e4d-a259-63451ba6dae4+elastic-apm-AustraliaEastwebspace |
    | WEBSITE_RESOURCE_GROUP | resource_group                                                         |
    | WEBSITE_SITE_NAME      | site_name                                                              |
    When cloud metadata is collected
    Then cloud metadata is null