#!/usr/bin/env bash
set -u
export PATH=/home/deedub/.nvm/versions/node/v22.18.0/bin:/usr/bin:/bin
cd /mnt/e/Factorio2 || exit 1
name="$1"
shift
"$@" > "docs/evidence/p5-02-2026-09-07/$name.log" 2>&1
result=$?
tail -24 "docs/evidence/p5-02-2026-09-07/$name.log"
echo "EXIT_CODE=$result"
exit "$result"
