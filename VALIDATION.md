# SQL Server edition validation — 2026-10-08

Executed here:

- .NET10 solution restore and build: 0 warnings, 0 errors.
- EF10 initial SQL Server migration generated; model snapshot has no pending changes.
- Idempotent SQL Server migration script generated: database/FoodHubSqlServer.sql.
- ModelChecks passed: SQL Server provider, six entity types, foreign keys/delete behavior, unique checkout index, commission precision, Guid concurrency token and order business rules.
- Nearby SQL Server query composes with server-side OFFSET/FETCH pagination and explicit parameters (translation-only check).

Not executed here:

- No running SQL Server instance is available. SQL Server migration application, seeding and live API/browser workflows were NOT executed for this edition.
- Windows .cmd scripts were reviewed, not executed in this Linux environment.
- No production deployment, capacity benchmark or full security assessment.

The Angular source is unchanged from the earlier build-tested version. Screenshots are historical UI captures from that version using SQLite; they are NOT evidence of SQL Server execution. SQLite provider/runtime and old additive schema upgrade are removed from this edition.

To run offline model/translation checks:

```bat
dotnet run --project tests/FoodHub.ModelChecks
```

After starting the API against a NEW LOCAL TEST SQL Server database, run integration checks (creates fixture data, never use production):

```bat
python tests/nearby_workflow.py http://localhost:5180
```

This script first runs the order/commission/ownership workflow, then nearby fixtures. Do not run repeatedly against a shared demo database expecting clean totals. Inspect any failures; live SQL Server behavior remains unverified here.

ZIP contains source, lockfile, migrations, SQL script and setup guides. Excludes databases, installed dependencies and generated build output.
