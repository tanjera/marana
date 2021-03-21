### Bugs

### Short-Term To-Do: For Programming

- Add status to instructions to allow for easy liquidation
  - E.g. Enum in Instructions to check before buying order

- Track success or failure of batch scripts to check for hanging on library update

- Print summary of placed buy/sell orders on "execute"

- Add default color to Settings/config.cfg
  
- Add volume indicators!!!

- Send notification emails on success and/or failure
  - Include Setting for notification frequency (Daily, Weekly, OnFail)
  - Include Settings for email accounts to originate notifications from, to
  - Log Execution batches to a database table
  - Log all Signals and Orders placed to database table
  - On "marana notify send" (can be called separate PID from execution in case of core dump)
    - Go through the database, collect successful and/or failed Executions
    - Send email notifying of success or failure to run Executions
      - Can also include Signals, Orders placed


### Goals