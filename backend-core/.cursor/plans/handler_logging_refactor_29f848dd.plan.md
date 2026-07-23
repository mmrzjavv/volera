---
name: Handler Logging Refactor
overview: Introduce consistent, minimal logging for all MediatR command and query handlers via a single pipeline behavior that logs action, user, entity/identifier affected, success/failure, and duration. Remove ad-hoc Console.WriteLine from handlers and domain event handler. Use structured ILogger and an optional current-user provider so no handler code or request bodies are logged.
todos: []
isProject: false
---

# Handler logging refactor and enhancem