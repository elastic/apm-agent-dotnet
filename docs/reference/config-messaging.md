---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/config-messaging.html
---

# Messaging configuration options [config-messaging]


## `IgnoreMessageQueues` ([1.10]) [config-ignore-message-queues]

Used to filter out specific messaging queues/topics/exchanges from being traced. When set, sends-to and receives-from the specified queues/topics/exchanges will be ignored.

This config accepts a comma separated string of wildcard patterns of queues/topics/exchange names which should be ignored.

The wildcard, `*`, matches zero or more characters, and matching is case insensitive by default. Prepending an element with `(?-i)` makes the matching case sensitive. Examples: `/foo/*/bar/*/baz*`, `*foo*`.

| Default | Type |
| --- | --- |
| <empty string> | String |

| Environment variable name | IConfiguration or Web.config key |
| --- | --- |
| `ELASTIC_APM_IGNORE_MESSAGE_QUEUES` | `ElasticApm:IgnoreMessageQueues` |

