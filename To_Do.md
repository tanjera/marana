### Bugs


### Short-Term To-Do: For Programming
- **Alpaca.Paper** does not support adjusted (split) closing prices
  - Once on a Live account, will need to implement Polygon's adjusted closing prices!
- **Add technical indicators**  
  - MACD: https://www.investopedia.com/terms/m/macd.asp
  - Stochastic Oscillator
  - ROC
  - MFI
- **Add indices** S&P? For background understanding of market conditions
- **Add confidence input** Human input for confidence of market conditions
- Check _validity based on when market was last open!
- Implement error log file, collect all exceptions/errors


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
  - #1 SMA7 > SMA20 > SMA50 ?> SMA100 ?> SMA200 (indicates growth steady!)
    - **TO_DO**: **Analyze using variance Signal**
  - #2 Being at or near within resistance range e.g. 1m high/low (not too high!)
  - #3 Crossovers in longer-term averages (SMA20-50, SMA50-100, etc.)
  - History of good performance (6 mo, 1 yr)
  - Positive short-metric crossovers (SMA7-20 and SMA20-50) preceded by troughs in longer-term metrics?
    - Could indicate a breakout?


#### "B-Score" (https://www.reddit.com/r/algotrading/comments/ldkt1z/options_trading_with_automated_ta/)
- Here are all the checks for B-Score. If they are True, the counter gets increased by 1.
- RSI <=40
- Volume >=100
- Filled price <= Lower Bollinger band
- SMA ( 5 days) <= VWAP
- Spread >=0.05 (This might change in future)
- Filled price = Current Bid
- IV<=40
- Today gain <= 0


### Goals