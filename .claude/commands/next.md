Read docs/PROGRESS.md.

Find the first task in the table whose status is not "done". Call it the current
task.

Then follow these rules in order.

1. If the current task's status is "blocked":
   Print the task id, the task text, and what it is blocked by. Then stop. Do
   nothing else.

2. If the current task's owner is "human":
   Print the task id, the task text, and the evidence file it needs. Print the
   section heading in that evidence file where the result goes. Then print:
   "This one is yours. Tell me the result when you have it and I will write it
   in." Then stop. Do nothing else.

3. If the current task's owner is "claude" and its status is "todo" or "doing":
   a. Set its status to "doing" in docs/PROGRESS.md and commit that one-line
      change.
   b. Check the task's "blocked by" column. If it names decision rows, open
      docs/DECISIONS.md and confirm every one of them has status "decided" with
      "decided by", "on" and "via" filled in. If any is not decided, set the
      task's status back to "todo", print which rows are missing, and stop.
   c. Do the task. Follow the prompt file for it if one is named in the task
      text. If no prompt file is named, follow the constitution's rules for a
      milestone: build, then run the cheap checks, then write the evidence file.
   d. Write the evidence file named in the task's "evidence" column.
   e. Run the cheap checks: npm test, npm run typecheck, npm run lint, npm run
      docsync:check. If any is red, fix it before continuing.
   f. Set the task's status to "done", fill in "done on" with today's date, add
      a log line at the bottom of docs/PROGRESS.md naming the evidence, and
      regenerate the "waiting on the human" list.
   g. Commit everything.
   h. Print, in this order: what you built; the measured numbers; anything a
      human must decide; the file-by-file change list.
   i. Then stop. Do not start the next task.

Never mark a task with owner "human" as done. Only the human may do that.
Never skip a task to reach a later one.
