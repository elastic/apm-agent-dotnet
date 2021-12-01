Feature: Outcome

  Background: An agent with default configuration
    Given an agent

  # ---- user set outcome

  Scenario: User set outcome on span has priority over instrumentation
    Given an active span
    And the agent sets the span outcome to 'success'
    And a user sets the span outcome to 'failure'
    When the span ends
    Then the span outcome is 'failure'

  Scenario: User set outcome on transaction has priority over instrumentation
    Given an active transaction
    And the agent sets the transaction outcome to 'failure'
    And a user sets the transaction outcome to 'unknown'
    When the transaction ends
    Then the transaction outcome is 'unknown'

  # ---- span & transaction outcome from reported errors

  Scenario: span with error
    Given an active span
    And an error is reported to the span
    When the span ends
    Then the span outcome is 'failure'

  Scenario: span without error
    Given an active span
    When the span ends
    Then the span outcome is 'success'

  Scenario: transaction with error
    Given an active transaction
    And an error is reported to the transaction
    When the transaction ends
    Then the transaction outcome is 'failure'

  Scenario: transaction without error
    Given an active transaction
    When the transaction ends
    Then the transaction outcome is 'success'

  # ---- HTTP

  @http
  Scenario Outline: HTTP transaction and span outcome
    Given an active transaction 
    And a HTTP call is received that returns <status>
    When the transaction ends
    Then the transaction outcome is '<server>'
    Given an active span 
    And a HTTP call is made that returns <status>
    When the span ends
    Then the span outcome is '<client>'
    Examples:
      | status | client  | server  |
      | 100    | success | success |
      | 200    | success | success |
      | 300    | success | success |
      | 400    | failure | success |
      | 404    | failure | success |
      | 500    | failure | failure |
      | -1     | failure | failure |
      # last row with negative status represents the case where the status is not available
      # for example when an exception/error is thrown without status (IO error, redirect loop, ...)

  # ---- gRPC

  # reference spec : https://github.com/grpc/grpc/blob/master/doc/statuscodes.md

  @grpc
  Scenario Outline: gRPC transaction and span outcome
    Given an active transaction
    And a gRPC call is received that returns '<status>'
    When the transaction ends
    Then the transaction outcome is '<server>'
    Given an active span
    And a gRPC call is made that returns '<status>'
    When the span ends
    Then the span outcome is '<client>'
    Examples:
      | status              | client  | server  |
      | OK                  | success | success |
      | CANCELLED           | failure | success |
      | UNKNOWN             | failure | failure |
      | INVALID_ARGUMENT    | failure | success |
      | DEADLINE_EXCEEDED   | failure | failure |
      | NOT_FOUND           | failure | success |
      | ALREADY_EXISTS      | failure | success |
      | PERMISSION_DENIED   | failure | success |
      | RESOURCE_EXHAUSTED  | failure | failure |
      | FAILED_PRECONDITION | failure | failure |
      | ABORTED             | failure | failure |
      | OUT_OF_RANGE        | failure | success |
      | UNIMPLEMENTED       | failure | success |
      | INTERNAL            | failure | failure |
      | UNAVAILABLE         | failure | failure |
      | DATA_LOSS           | failure | failure |
      | UNAUTHENTICATED     | failure | success |
      | n/a                 | failure | failure |
    # last row with 'n/a' status represents the case where status is not available
