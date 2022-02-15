Feature: APM server authentication with API key and secret token

  Scenario: A configured API key is sent in the Authorization header
    Given an agent configured with
      | setting    | value        |
      | api_key    | RTNxMjlXNEJt |
    When the agent sends a request to APM server
    Then the Authorization header of the request is 'ApiKey RTNxMjlXNEJt'

  Scenario: A configured secret token is sent in the Authorization header
    Given an agent configured with
      | setting       | value         |
      | secret_token  | secr3tT0ken   |
    When the agent sends a request to APM server
    Then the Authorization header of the request is 'Bearer secr3tT0ken'

  Scenario: A configured API key takes precedence over a secret token
    Given an agent configured with
      | setting       | value         |
      | api_key       | MjlXNEJasdfDt |
      | secret_token  | secr3tT0ken   |
    When the agent sends a request to APM server
    Then the Authorization header of the request is 'ApiKey MjlXNEJasdfDt'

