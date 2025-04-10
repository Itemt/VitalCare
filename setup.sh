#!/bin/bash
set -e

# Wait for SQL Server to start
sleep 30s

# Check if SQL Server is ready
function wait_for_sql() {
  for i in {1..50}; do
    /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $MSSQL_SA_PASSWORD -Q "SELECT 1" &> /dev/null
    if [ $? -eq 0 ]; then
      echo "SQL Server is ready"
      return 0
    fi
    echo "Waiting for SQL Server to start..."
    sleep 1
  done
  echo "Timed out waiting for SQL Server to start"
  exit 1
}

# Run the initialization script
function initialize_db() {
  echo "Initializing database..."
  /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $MSSQL_SA_PASSWORD -i /docker-entrypoint-initdb.d/init.sql
  echo "Database initialization completed"
}

# Wait for SQL Server to be ready
wait_for_sql

# Run the initialization script
initialize_db

# Keep container running
tail -f /dev/null
