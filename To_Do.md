### Bugs
- Hangs on "Requesting Data" on library update from Alphavantage...
  - Attampted to create Task timeout race condition using Task.WhenAny()
    - Fails because AlphaVantage.GetData_Daily() is return type Task<object> but WhenAny returns Task<void>
      - Cannot differentiate return types of Task<> using traditional methods
  - Attempted to create Task timeout race condition using Task.Start
    - Manually started the Tasks, used a while(no tasks completed) { await Task.Delay(); }
    - Fails because cannot call Task.Start on a Task<object>; only on Task<void>

### Short-Term To-Do: For Programming
- Track success or failure of batch scripts to check for hanging on library update

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