# FoodHub — SQL Server edition

Complete learning MVP: ASP.NET Core10 controller-based Web API, EF Core10 Code First, SQL Server and Angular20. Read START-HERE.md for setup and migrations. SQLite runtime/provider and ad-hoc schema upgrade are removed.

Features: JWT customer/admin/restaurant roles, ownership restriction, menu, nearby search, one-restaurant cart, COD orders, checkout idempotency, price/commission snapshots, status history, concurrency checks and delivered-order commission summary.

Database: explicit keys/relationships/delete behavior, unique emails/owner/idempotency keys, bounded string columns, commission decimal precision, money/coordinate check constraints and search/order indexes. One restaurant record represents one branch.

Initial migration and generated idempotent SQL script are included. Development startup migrates and seeds demo users; production requires reviewed migration deployment and secure configuration. Never mix EnsureCreated with this migration workflow.

This edition keeps .NET10; it does not downgrade frameworks. Separate Angular TS/HTML/SCSS source remains.

Limitations: no rider tracking, map/geocoding provider, live payments, refunds or actual payouts. See VALIDATION.md for exact executed checks. No production load/capacity guarantee.
