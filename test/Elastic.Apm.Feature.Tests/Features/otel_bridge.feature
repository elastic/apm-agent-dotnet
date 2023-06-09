@opentelemetry-bridge
Feature: OpenTelemetry bridge

  # --- Creating Elastic span or transaction from OTel span

  Scenario: Create transaction from OTel span with remote context
    Given an agent
    And OTel span is created with remote context as parent
    Then Elastic bridged object is a transaction
    Then Elastic bridged transaction has remote context as parent

  Scenario: Create root transaction from OTel span without parent
    Given an agent
    And OTel span is created without parent
    And OTel span ends
    Then Elastic bridged object is a transaction
    Then Elastic bridged transaction is a root transaction
    # outcome should not be inferred from the lack/presence of errors
    Then Elastic bridged transaction outcome is "unknown"

  Scenario: Create span from OTel span
    Given an agent
    And OTel span is created with local context as parent
    And OTel span ends
    Then Elastic bridged object is a span
    Then Elastic bridged span has local context as parent
    # outcome should not be inferred from the lack/presence of errors
    Then Elastic bridged span outcome is "unknown"

  # --- OTel span kind mapping for spans & transactions

  Scenario Outline: OTel span kind <kind> for spans & default span type & subtype
    Given an agent
    And an active transaction
    And OTel span is created with kind "<kind>"
    And OTel span ends
    Then Elastic bridged object is a span
    Then Elastic bridged span OTel kind is "<kind>"
    Then Elastic bridged span type is "<default_type>"
    Then Elastic bridged span subtype is "<default_subtype>"
    Examples:
      | kind     | default_type | default_subtype |
      | INTERNAL | app          | internal        |
      | SERVER   | unknown      |                 |
      | CLIENT   | unknown      |                 |
      | PRODUCER | unknown      |                 |
      | CONSUMER | unknown      |                 |

  Scenario Outline: OTel span kind <kind> for transactions & default transaction type
    Given an agent
    And OTel span is created with kind "<kind>"
    And OTel span ends
    Then Elastic bridged object is a transaction
    Then Elastic bridged transaction OTel kind is "<kind>"
    Then Elastic bridged transaction type is 'unknown'
    Examples:
      | kind     |
      | INTERNAL |
      | SERVER   |
      | CLIENT   |
      | PRODUCER |
      | CONSUMER |

  # OTel span status mapping for spans & transactions

  Scenario Outline:  OTel span mapping with status <status> for transactions
    Given an agent
    And OTel span is created with kind 'SERVER'
    And OTel span status set to "<status>"
    And OTel span ends
    Then Elastic bridged object is a transaction
    Then Elastic bridged transaction outcome is "<outcome>"
    Then Elastic bridged transaction result is not set
    Examples:
      | status | outcome |
      | unset  | unknown |
      | ok     | success |
      | error  | failure |

  Scenario Outline:  OTel span mapping with status <status> for spans
    Given an agent
    Given an active transaction
    And OTel span is created with kind 'INTERNAL'
    And OTel span status set to "<status>"
    And OTel span ends
    Then Elastic bridged object is a span
    Then Elastic bridged span outcome is "<outcome>"
    Examples:
      | status | outcome |
      | unset  | unknown |
      | ok     | success |
      | error  | failure |

  # --- span type, subtype and action inference from OTel attributes

  # --- HTTP server
  # https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/http.md#http-server
  Scenario Outline: HTTP server [ <http.url> <http.scheme> ]
    Given an agent
    And OTel span is created with kind 'SERVER'
    And OTel span has following attributes
      | http.url          | <http.url>      |
      | http.scheme       | <http.scheme>   |
    And OTel span ends
    Then Elastic bridged object is a transaction
    Then Elastic bridged transaction type is "request"
    Examples:
      | http.url                | http.scheme |
      | http://testing.invalid/ |             |
      |                         | http        |

  # --- HTTP client
  # https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/http.md#http-client
  Scenario Outline: HTTP client [ <http.url> <http.scheme> <http.host> <net.peer.ip> <net.peer.name> <net.peer.port> ]
    Given an agent
    And an active transaction
    And OTel span is created with kind 'CLIENT'
    And OTel span has following attributes
      | http.url          | <http.url>      |
      | http.scheme       | <http.scheme>   |
      | http.host         | <http.host>     |
      | net.peer.ip       | <net.peer.ip>   |
      | net.peer.name     | <net.peer.name> |
      | net.peer.port     | <net.peer.port> |
    And OTel span ends
    Then Elastic bridged span type is 'external'
    Then Elastic bridged span subtype is 'http'
    Then Elastic bridged span OTel attributes are copied as-is
    Then Elastic bridged span destination resource is set to "<target_service_name>"
    Then Elastic bridged span service target type is 'http' and name is "<target_service_name>"
    Examples:
      | http.url                      | http.scheme | http.host       | net.peer.ip | net.peer.name | net.peer.port | target_service_name  |
      | https://testing.invalid:8443/ |             |                 |             |               |               | testing.invalid:8443 |
      | https://[::1]/                |             |                 |             |               |               | [::1]:443            |
      | http://testing.invalid/       |             |                 |             |               |               | testing.invalid:80   |
      |                               | http        | testing.invalid |             |               |               | testing.invalid:80   |
      |                               | https       | testing.invalid | 127.0.0.1   |               |               | testing.invalid:443  |
      |                               | http        |                 | 127.0.0.1   |               | 81            | 127.0.0.1:81         |
      |                               | https       |                 | 127.0.0.1   |               | 445           | 127.0.0.1:445        |
      |                               | http        |                 | 127.0.0.1   | host1         | 445           | host1:445            |
      |                               | https       |                 | 127.0.0.1   | host2         | 445           | host2:445            |

  # --- DB client
  # https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/database.md
  Scenario Outline: DB client [ <db.system> <net.peer.ip> <net.peer.name> <net.peer.port>]
    Given an agent
    And an active transaction
    And OTel span is created with kind 'CLIENT'
    And OTel span has following attributes
      | db.system         | <db.system>     |
      | db.name           | <db.name>       |
      | net.peer.ip       | <net.peer.ip>   |
      | net.peer.name     | <net.peer.name> |
      | net.peer.port     | <net.peer.port> |
    And OTel span ends
    Then Elastic bridged span type is 'db'
    Then Elastic bridged span subtype is "<db.system>"
    Then Elastic bridged span OTel attributes are copied as-is
    Then Elastic bridged span destination resource is set to "<resource>"
    Then Elastic bridged span service target type is "<db.system>" and name is "<target_service_name>"
    Examples:
      | db.system | db.name | net.peer.ip | net.peer.name | net.peer.port | resource    | target_service_name |
      | mysql     |         |             |               |               | mysql       |                     |
      | oracle    |         |             | oracledb      |               | oracle      |                     |
      | oracle    |         | 127.0.0.1   |               |               | oracle      |                     |
      | mysql     |         | 127.0.0.1   | dbserver      | 3307          | mysql       |                     |
      | mysql     | myDb    |             |               |               | mysql/myDb  | myDb                |
      | oracle    | myDb    |             | oracledb      |               | oracle/myDb | myDb                |
      | oracle    | myDb    | 127.0.0.1   |               |               | oracle/myDb | myDb                |
      | mysql     | myDb    | 127.0.0.1   | dbserver      | 3307          | mysql/myDb  | myDb                |

  # --- Messaging consumer (transaction consuming/receiving a message)
  # https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/messaging.md
  Scenario: Messaging consumer
    Given an agent
    And OTel span is created with kind 'CONSUMER'
    And OTel span has following attributes
      | messaging.system  | anything |
    And OTel span ends
    Then Elastic bridged transaction type is 'messaging'

  # --- Messaging producer (client span emitting a message)
  # https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/messaging.md
  Scenario Outline: Messaging producer [ <messaging.system> <messaging.destination> <messaging.url> <net.peer.ip> <net.peer.name> <net.peer.port>]
    Given an agent
    And an active transaction
    And OTel span is created with kind 'PRODUCER'
    And OTel span has following attributes
      | messaging.system       | <messaging.system>      |
      | messaging.destination  | <messaging.destination> |
      | messaging.url          | <messaging.url>         |
      | net.peer.ip            | <net.peer.ip>           |
      | net.peer.name          | <net.peer.name>         |
      | net.peer.port          | <net.peer.port>         |
    And OTel span ends
    Then Elastic bridged span type is 'messaging'
    Then Elastic bridged span subtype is "<messaging.system>"
    Then Elastic bridged span OTel attributes are copied as-is
    Then Elastic bridged span destination resource is set to "<resource>"
    Then Elastic bridged span service target type is "<messaging.system>" and name is "<target_service_name>"
    Examples:
      | messaging.system | messaging.destination | messaging.url         | net.peer.ip | net.peer.name | net.peer.port | resource         | target_service_name |
      | rabbitmq         |                       | amqp://carrot:4444/q1 |             |               |               | rabbitmq         |                     |
      | rabbitmq         |                       |                       | 127.0.0.1   | carrot-server | 7777          | rabbitmq         |                     |
      | rabbitmq         |                       |                       |             | carrot-server |               | rabbitmq         |                     |
      | rabbitmq         |                       |                       | 127.0.0.1   |               |               | rabbitmq         |                     |
      | rabbitmq         | myQueue               | amqp://carrot:4444/q1 |             |               |               | rabbitmq/myQueue | myQueue             |
      | rabbitmq         | myQueue               |                       | 127.0.0.1   | carrot-server | 7777          | rabbitmq/myQueue | myQueue             |
      | rabbitmq         | myQueue               |                       |             | carrot-server |               | rabbitmq/myQueue | myQueue             |
      | rabbitmq         | myQueue               |                       | 127.0.0.1   |               |               | rabbitmq/myQueue | myQueue             |

  # --- RPC client
  # https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/rpc.md
  Scenario Outline: RPC client [ <rpc.system>  <rpc.service> <net.peer.ip> <net.peer.name> <net.peer.port>]
    Given an agent
    And an active transaction
    And OTel span is created with kind 'CLIENT'
    And OTel span has following attributes
      | rpc.system    | <rpc.system>    |
      | rpc.service   | <rpc.service>   |
      | net.peer.ip   | <net.peer.ip>   |
      | net.peer.name | <net.peer.name> |
      | net.peer.port | <net.peer.port> |
    And OTel span ends
    Then Elastic bridged span type is 'external'
    Then Elastic bridged span subtype is "<rpc.system>"
    Then Elastic bridged span OTel attributes are copied as-is
    Then Elastic bridged span destination resource is set to "<resource>"
    Then Elastic bridged span service target type is "<rpc.system>" and name is "<target_service_name>"
    Examples:
      | rpc.system | rpc.service | net.peer.ip | net.peer.name | net.peer.port | resource        | target_service_name |
      | grpc       |             |             |               |               | grpc            |                     |
      | grpc       | myService   |             |               |               | myService       | myService           |
      | grpc       | myService   |             | rpc-server    |               | rpc-server      | rpc-server          |
      | grpc       | myService   | 127.0.0.1   | rpc-server    |               | rpc-server      | rpc-server          |
      | grpc       |             | 127.0.0.1   | rpc-server    | 7777          | rpc-server:7777 | rpc-server:7777     |
      | grpc       | myService   | 127.0.0.1   | rpc-server    | 7777          | rpc-server:7777 | rpc-server:7777     |
      | grpc       | myService   | 127.0.0.1   |               | 7777          | 127.0.0.1:7777  | 127.0.0.1:7777      |

  # --- RPC server
  # https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/rpc.md
  Scenario: RPC server
    Given an agent
    And OTel span is created with kind 'SERVER'
    And OTel span has following attributes
      | rpc.system | grpc |
    And OTel span ends
    Then Elastic bridged transaction type is 'request'

