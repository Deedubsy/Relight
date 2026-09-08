#!/usr/bin/env bash
set -u
export PATH=/home/deedub/.nvm/versions/node/v22.18.0/bin:/usr/bin:/bin
cd /mnt/e/Factorio2 || exit 1
name="$1"
shift
log="docs/evidence/p6-04-2026-09-07/$name.log"
if [ -e "$log" ]; then echo "Refusing to overwrite review evidence: $log"; exit 2; fi
"$@" > "$log" 2>&1
result=$?
tail -24 "$log"
echo "EXIT_CODE=$result"
exit "$result"
