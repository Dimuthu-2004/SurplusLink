# Backend CI and viva explanation

The workflow is [backend-ci.yml](../.github/workflows/backend-ci.yml). It runs
on every push to `main` and every pull request targeting `main`, including new
commits pushed to an open pull request. It has no path filters: even a change
elsewhere in this monorepo checks backend compatibility. A push to another
branch without a pull request targeting `main` does not trigger this workflow.

| Configuration or step | Explanation for viva |
| --- | --- |
| `name: Backend CI` | Gives the workflow a recognizable name in GitHub Actions. |
| `push` / `pull_request`, `branches: [main]` | Checks changes pushed to main and proposed changes before merging into main. For pull requests, the branch filter refers to the target branch. |
| `permissions: contents: read` | Lets the job read the repository without granting write permission. |
| `runs-on: ubuntu-24.04` | Uses a fresh GitHub-hosted Linux runner. Linux runners support PostgreSQL service containers. |
| `timeout-minutes: 15` | Stops a stalled job instead of letting it run indefinitely. |
| PostgreSQL service | Starts a temporary database server for the existing database integration tests. The service is removed when the job ends. |
| `pg_isready` health check | Waits for PostgreSQL to accept connections before running the job steps, avoiding startup timing failures. |
| `5432:5432` | Publishes the container's database port on the runner, allowing the tests to connect to `localhost:5432`. |
| `SURPLUSLINK_TEST_CONNECTION` | Supplies the exact environment variable read by the integration-test fixture. Without it, PostgreSQL tests are skipped. |
| Checkout | Downloads the repository into the runner's workspace, including the solution, projects, tests and SDK configuration. |
| Setup .NET | Installs SDK `8.0.423` from `global.json`. This matches the SDK selected in the development workspace and documented in existing project evidence. |
| Restore | `dotnet restore SurplusLink.sln` downloads NuGet dependencies for both backend projects. |
| Build | `dotnet build SurplusLink.sln --configuration Release --no-restore` compiles the API and tests in Release configuration. Restore has already succeeded, so repeating it is unnecessary. |
| Test | `dotnet test SurplusLink.sln --configuration Release --no-build --no-restore` runs the compiled Release tests. It reuses the build output so the tested code is exactly what the preceding step built. |

Steps run in order. A failed restore, build or test command fails the job, and
later steps do not run by default. The workflow performs validation; it does
not deploy the API. Branch protection must require its check if failed CI
should block merging.

## Why the SDK is pinned

Both `.csproj` files target `net8.0`. That is a target framework, not an exact
SDK version. Before this change, the repository had no `global.json` and the old
CI job selected `8.0.x`. The workspace's `dotnet --version` reports `8.0.423`,
also recorded in [existing testing evidence](evidence/shared/member1-s1-s2-s4-testing.md).

The root [global.json](../global.json) now selects `8.0.423` with
`rollForward: disable` and `allowPrerelease: false`. This prevents the CLI from
silently selecting a different installed SDK. Developers must have that SDK
installed; future SDK upgrades should update this one file. The setup action
reads the same file rather than maintaining another version number in YAML.

## Why PostgreSQL is necessary

`PostgresFactAttribute` skips database tests unless `SURPLUSLINK_TEST_CONNECTION`
is set. `RequirementsDatabase` in
`backend/SurplusLink.Tests/BuyerRequirementsIntegrationTests.cs` connects to the
`postgres` database, creates uniquely named disposable test databases, applies
migrations, seeds test data and drops its databases afterwards. These tests
exercise PostgreSQL constraints, role integrity, matching, history and concurrent
updates, so a database substitute would not verify the same behavior.

The service uses PostgreSQL 18, matching the major version in the project's
existing local test evidence. The container's `postgres` user has the privileges
the fixture needs to create/drop databases and install migration extensions.
`surpluslink_ci_only` is a public, disposable test password, not a development
or production credential. No repository secrets are needed, including for fork
pull requests. No separate migration step is needed because the fixture owns
migration setup inside its temporary databases.

The test requiring both a live FastAPI service and PostgreSQL remains skipped
unless `SURPLUSLINK_AI_TEST_URL` and `AI_SERVICE_SHARED_TOKEN` are also supplied.
This backend workflow supplies PostgreSQL only. The existing Python workflow
continues to run the AI-service tests separately; neither suite alone claims to
verify that live cross-service test.

The old backend job was removed from `ci.yml` to avoid duplicate backend runs.
Its web and AI-service jobs retain their existing triggers and commands.

## Short viva answer

“When code is pushed to main or a pull request targets main, GitHub starts a
clean Linux runner and a temporary PostgreSQL server. It checks out our code,
installs our exact .NET SDK, restores NuGet packages, builds the solution in
Release mode and runs its tests. PostgreSQL is included because our existing
tests verify real migrations, constraints and concurrency. A failed step marks
CI as failed, so we can catch backend regressions before merging.”

References: [setup-dotnet](https://github.com/actions/setup-dotnet),
[GitHub PostgreSQL services](https://docs.github.com/en/actions/tutorials/use-containerized-services/create-postgresql-service-containers),
[workflow syntax](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax).
