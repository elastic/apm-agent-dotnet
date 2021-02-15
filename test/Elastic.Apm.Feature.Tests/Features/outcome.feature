Feature: Outcome

  # ---- user set outcome

  Scenario: User set outcome on span has priority over instrumentation
    Given an agent
    And an active span
    And user sets span outcome to 'failure'
    And span terminates with outcome 'success'
    Then span outcome is 'failure'

  Scenario: User set outcome on transaction has priority over instrumentation
    Given an agent
    And an active transaction
    And user sets transaction outcome to 'unknown'
    And transaction terminates with outcome 'failure'
    Then transaction outcome is 'unknown'

  # ---- span & transaction outcome from reported errors

  Scenario: span with error
    Given an agent
    And an active span
    And span terminates with an error
    Then span outcome is 'failure'

  Scenario: span without error
    Given an agent
    And an active span
    And span terminates without error
    Then span outcome is 'success'

  Scenario: transaction with error
    Given an agent
    And an active transaction
    And transaction terminates with an error
    Then transaction outcome is 'failure'

  Scenario: transaction without error
    Given an agent
    And an active transaction
    And transaction terminates without error
    Then transaction outcome is 'success'

  # ---- HTTP

  @http
  Scenario Outline: HTTP transaction and span outcome
    Given an agent
    And an HTTP transaction with <status> response code
    Then transaction outcome is "<server>"
    Given an HTTP span with <status> response code
    Then span outcome is "<client>"
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
    Given an agent
    And a gRPC transaction with '<status>' status
    Then transaction outcome is "<server>"
    Given a gRPC span with '<status>' status
    Then span outcome is "<client>"
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
