INSERT INTO `TSD` (
    Asset, Symbol, Date, Open, High, Low, Close, Volume,
    SMA7, SMA20, SMA50, SMA100, SMA200,
    EMA7, EMA20, EMA50, DEMA7, DEMA20, DEMA50, TEMA7, TEMA20, TEMA50,
    RSI,
    MACD, MACD_Histogram, MACD_Signal,
    BollingerBands_Center, BollingerBands_Upper, BollingerBands_Lower, BollingerBands_Percent, BollingerBands_ZScore, BollingerBands_Width
    ) VALUES {values};