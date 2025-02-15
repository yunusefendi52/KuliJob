#!/bin/bash

set -e

dotnet pack src/KuliJob -o artifacts &
dotnet pack src/KuliJob.Dashboard -o artifacts &
dotnet pack src/KuliJob.Postgres -o artifacts &
dotnet pack src/KuliJob.Sqlite -o artifacts &

wait
