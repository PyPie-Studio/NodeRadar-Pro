---
name: superpowers-rest-automation
description: REST API integrations, updater checks (GitHub Releases API), and webhook alerting.
---

# superpowers-rest-automation

## Guidelines
- Always configure strict timeouts (`HttpClient.Timeout = TimeSpan.FromSeconds(10)`).
- Reuse `HttpClient` instances via `IHttpClientFactory` or static singleton.
- Validate SSL/TLS certificates and avoid insecure certificate bypasses.
