Feature: Api Key

	Scenario: A configured api key is sent in the Authorization header
		Given an agent
		When an api key is set to 'RTNxMjlXNEJt' in the config
		Then the Authorization header is 'ApiKey RTNxMjlXNEJt'

	Scenario: A configured api key takes precedence over a secret token
		Given an agent
		When an api key is set in the config
		And a secret_token is set in the config
		Then the api key is sent in the Authorization header

	Scenario: A configured secret token is sent if no api key is configured
		Given an agent
		When a secret_token is set in the config
		And an api key is not set in the config
		Then the secret token is sent in the Authorization header