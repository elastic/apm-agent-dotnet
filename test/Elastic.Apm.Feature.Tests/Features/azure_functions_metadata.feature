Feature: Extracting Metadata for Azure Function Apps

  Background:
    Given an agent configured with
      | setting        | value |
      | cloud_provider | azure |

  Scenario Outline: Azure Function App with minimum set of environment variables present in expected format
    Given the following environment variables are present
      | name                        | value                                                                   |
      | FUNCTIONS_EXTENSION_VERSION | version                                                                 |
      | WEBSITE_OWNER_NAME          | d2cd53b3-acdc-4964-9563-3f5201556a81+faas_group-CentralUSwebspace-Linux |
      | WEBSITE_SITE_NAME           | site_name                                                               |
    When cloud metadata is collected
    Then cloud metadata is not null
    And cloud metadata 'account.id' is 'd2cd53b3-acdc-4964-9563-3f5201556a81'
    And cloud metadata 'provider' is 'azure'
    And cloud metadata 'service.name' is 'functions'
    And cloud metadata 'instance.name' is 'site_name'
    And cloud metadata 'project.name' is 'faas_group'
    And cloud metadata 'region' is 'CentralUS'

  Scenario Outline: Azure Function App with typical set of environment variables present in expected format
    Given the following environment variables are present
      | name                        | value                                                                   |
      | FUNCTIONS_EXTENSION_VERSION | version                                                                 |
      | WEBSITE_OWNER_NAME          | d2cd53b3-acdc-4964-9563-3f5201556a81+faas_group-CentralUSwebspace-Linux |
      | WEBSITE_SITE_NAME           | site_name                                                               |
      | REGION_NAME                 | Central US                                                              |
      | WEBSITE_RESOURCE_GROUP      | faas_group_from_env                                                     |
    When cloud metadata is collected
    Then cloud metadata is not null
    And cloud metadata 'account.id' is 'd2cd53b3-acdc-4964-9563-3f5201556a81'
    And cloud metadata 'provider' is 'azure'
    And cloud metadata 'service.name' is 'functions'
    And cloud metadata 'instance.name' is 'site_name'
    And cloud metadata 'project.name' is 'faas_group_from_env'
    And cloud metadata 'region' is 'Central US'

  Scenario: WEBSITE_OWNER_NAME environment variable not expected format
    Given the following environment variables are present
      | name               | value                                                                   |
      | WEBSITE_OWNER_NAME | d2cd53b3-acdc-4964-9563-3f5201556a81-faas_group-CentralUSwebspace-Linux |
      | WEBSITE_SITE_NAME  | site_name                                                               |
    When cloud metadata is collected
    Then cloud metadata is null

  Scenario: Missing FUNCTIONS_EXTENSION_VERSION environment variable
    Given the following environment variables are present
      | name               | value                                                                   |
      | WEBSITE_OWNER_NAME | d2cd53b3-acdc-4964-9563-3f5201556a81+faas_group-CentralUSwebspace-Linux |
      | WEBSITE_SITE_NAME  | site_name                                                               |
    When cloud metadata is collected
    Then cloud metadata is null

  Scenario: Missing WEBSITE_OWNER_NAME environment variable
    Given the following environment variables are present
      | name                        | value     |
      | FUNCTIONS_EXTENSION_VERSION | version   |
      | WEBSITE_SITE_NAME           | site_name |
    When cloud metadata is collected
    Then cloud metadata is null

  Scenario: Missing WEBSITE_SITE_NAME environment variable
    Given the following environment variables are present
      | name                        | value                                                                   |
      | FUNCTIONS_EXTENSION_VERSION | version                                                                 |
      | WEBSITE_OWNER_NAME          | d2cd53b3-acdc-4964-9563-3f5201556a81+faas_group-CentralUSwebspace-Linux |
    When cloud metadata is collected
    Then cloud metadata is null
