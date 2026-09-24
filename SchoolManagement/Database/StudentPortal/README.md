# Student portal database scripts

These idempotent SQL Server scripts were applied to the configured SchoolManagement database on 24 September 2026. They create the student portal, community and exam tables used by the backend models. Keep them with the backend for DB-first schema history and for another environment; do not rerun them against the same database unless needed.

Apply in order: student-portal.sql, student-community.sql, student-exams.sql.
