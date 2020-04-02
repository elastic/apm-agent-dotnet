Feature: API Key

  Scenario: A configured API key is sent in the Authorization header
    Given an agent
    When an api key is set to 'RTNxMjlXNEJt' in the config
    Then the Authorization header is 'ApiKey RTNxMjlXNEJt'

  Scenario: A configured API key takes precedence over a secret token
    Given an agent
    When an api key is set to 'MjlXNEJasdfDt' in the config
    And a secret_token is set to 'secr3tT0ken' in the config
    Then the Authorization header is 'ApiKey MjlXNEJasdfDt'

  Scenario: A configured secret token is sent if no API key is configured
    Given an agent
    When a secret_token is set to 'secr3tT0ken' in the config
    And an api key is not set in the config
    Then the Authorization header is 'Bearer secr3tT0ken'
