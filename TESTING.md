# TESTING.md

This document describes the testing strategy, infrastructure, and test cases for WebTransactions.

## Running the Tests

```bash
dotnet test
```

To run a specific test by name:

```bash
dotnet test --filter "FullyQualifiedName~CreateTransaction_ValidRequest"
```

---

## Test Strategy

The project uses **functional (integration) tests** rather than unit tests. Each test boots the full ASP.NET Core application in memory, sends real HTTP requests, and asserts on real HTTP responses. This means every layer of the application is exercised — routing, model validation, controllers, services, and the database — giving high confidence that the system behaves correctly end-to-end.

This approach was chosen to satisfy the assignment's requirement for functional automated testing. Unit tests that mock dependencies would verify individual methods in isolation but would not catch integration issues such as misconfigured routing, incorrect serialization, or broken database queries.

---

## Test Infrastructure

### CustomWebApplicationFactory

Located at `tests/WebTransactions.Api.Tests/CustomWebApplicationFactory.cs`.

`CustomWebApplicationFactory` extends `WebApplicationFactory<Program>` from `Microsoft.AspNetCore.Mvc.Testing`. It boots the full application with two important substitutions:

**1. In-memory SQLite database**

The real SQLite `.db` file is replaced with an in-memory SQLite database backed by an open `SqliteConnection`. This ensures:
- Tests do not read from or write to the production database
- The database starts clean for each factory instance
- No cleanup is needed after tests run

The connection is kept open for the lifetime of the factory (in-memory SQLite databases are destroyed when their connection closes) and disposed when the factory is disposed.

**2. Fake exchange rate service**

The real `ExchangeRateService` (which calls the Treasury API over HTTP) is replaced with `FakeExchangeRateService`, which returns a fixed exchange rate of `1.08`. This ensures:
- Tests do not make real HTTP calls to external services
- Tests are deterministic — the exchange rate never changes
- Tests run offline and are not affected by Treasury API availability

### FakeExchangeRateService

Located at `tests/WebTransactions.Api.Tests/FakeExchangeRateService.cs`.

A simple implementation of `IExchangeRateService` that returns a configurable fixed rate. The default rate is `1.08m`. Passing `null` simulates the scenario where no exchange rate is available within 6 months of the transaction date.

---

## Test Cases

All tests are in `tests/WebTransactions.Api.Tests/TransactionApiTests.cs`.

The test class uses `IClassFixture<CustomWebApplicationFactory>`, meaning one factory instance (and one in-memory database) is shared across all tests in the class.

---

### POST /api/transactions

#### `CreateTransaction_ValidRequest_Returns201WithId`

**What it tests:** The happy path for creating a transaction.

**Input:** A valid request with description `"Test purchase"`, date `2024-01-15`, and amount `100.50`.

**Expected outcome:** HTTP 201 Created, with a response body containing an `id` field.

---

#### `CreateTransaction_DescriptionTooLong_Returns400`

**What it tests:** Validation rejects descriptions longer than 50 characters.

**Input:** A request with a 51-character description.

**Expected outcome:** HTTP 400 Bad Request. The `[MaxLength(50)]` data annotation on `CreateTransactionRequest.Description` triggers ASP.NET Core's automatic model validation, which returns 400 without reaching the controller action.

---

#### `CreateTransaction_NegativeAmount_Returns400`

**What it tests:** Validation rejects non-positive amounts.

**Input:** A request with amount `-10`.

**Expected outcome:** HTTP 400 Bad Request. The `[Range(0.01, double.MaxValue)]` annotation on `CreateTransactionRequest.Amount` triggers automatic model validation.

---

### GET /api/transactions/{id}

#### `GetTransaction_ExistingTransaction_Returns200WithConvertedAmount`

**What it tests:** The happy path for retrieving a transaction with currency conversion.

**Setup:** A transaction is first created via POST, and its ID is extracted from the response.

**Input:** The transaction ID and currency `"Euro Zone-Euro"`.

**Expected outcome:** HTTP 200 OK, with `originalAmountUsd` of `100.00` and `convertedAmount` of `108.00` (100.00 × 1.08, the fixed fake exchange rate).

---

#### `GetTransaction_NonExistentId_Returns404`

**What it tests:** Retrieving a transaction that does not exist returns a 404.

**Input:** A randomly generated `Guid` that was never stored.

**Expected outcome:** HTTP 404 Not Found.

---

### GET /api/transactions

#### `GetAllTransactions_Returns200WithList`

**What it tests:** The list endpoint is reachable and returns a successful response.

**Expected outcome:** HTTP 200 OK.

---

## What Is Not Tested

- **Playwright UI tests** — end-to-end browser tests for the Blazor UI are planned as a separate test project (`tests/WebTransactions.UI.Tests`).
- **Treasury API integration** — the real exchange rate service is replaced with a fake in all current tests. A separate integration test (marked to run only in specific environments) would be needed to verify the live API call.
- **Performance and load testing** — explicitly out of scope per the project brief.
