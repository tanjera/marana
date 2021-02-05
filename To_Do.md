### Bugs

### Short-Term To-Do: For Programming
- Finish implementation of MySQL and Alpaca
  - **TO_DO** Check _validity based on when market was last open!
  - **TO_DO** Get symbols from Alpaca instead of Nasdaq Trader? May have NYSE...
  - **TO_DO** Implement _errors table, collect all exceptions/errors
  - ... also need to add exception handling for each MySQL connection.......
  - Continue implementation throughout Library.cs:    
    - Analysis can then access the DB for the entire pre-calculated dataset
      - **TO_DO** Switch data used in Analysis.Running to pull from database

### Short-Term To-Do: For Analysis
- NEED to determine where prices have been sitting (average highs and lows for 1 month)
  - **TO_DO**: Analyze into Signal.HasResistance; whether a signal takes place while within 2 standard deviations of the mean
- **TO_DO**: NEED to factor long/short term gains (% gain) over 1 yr, 6 mo, 1 mo when doing analysis
- Implement detection of variance in volume- vSMA7/20 with signal on change > 1 vMSD?
- Implement EMA7, EMA20
- Report summary for "analyze" with 1 symbol per line, with colored progress bar to indicate signals

### Analysis Strategies
- Reversals without crossovers (showing significant variation) are mostly meaningless signals
  - Especially in longer-term averages (50, 100, 200), it's small reversals in a general plateau
- Multiple crossovers on the same metric (especially SMA7-20) in a short period are not good
- Strongest signals are: 
  - #1 CMA7 > SMA20 > SMA50 ?> SMA100 ?> SMA200 (indicates growth steady!)
    - **TO_DO**: **Analyze using variance Signal**
  - #2 Being at or near within resistance range e.g. 1m high/low (not too high!)
  - #3 Crossovers in longer-term averages (SMA20-50, SMA50-100, etc.)
  - History of good performance (6 mo, 1 yr)
  - Positive short-metric crossovers (SMA7-20 and SMA20-50) preceded by troughs in longer-term metrics?
    - Could indicate a breakout?

### Goals